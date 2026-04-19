using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using glmusage.Converters;
using glmusage.Models;
using glmusage.Services;

namespace glmusage
{
    public partial class MainWindow : Window
    {
        private static readonly Color _colorSafe = Color.FromRgb(0x2E, 0xCC, 0x71);
        private static readonly Color _colorWarning = Color.FromRgb(0xF1, 0xC4, 0x0F);
        private static readonly Color _colorDanger = Color.FromRgb(0xE7, 0x4C, 0x3C);
        private static readonly Color _colorHover = Color.FromRgb(0xBF, 0x5A, 0xF2);
        private static readonly Color _colorGray = Color.FromRgb(0x8B, 0x94, 0x9E);
        private static readonly Color _colorMuted = Color.FromRgb(0x48, 0x4F, 0x58);
        private static readonly Color _colorSeparator = Color.FromRgb(0x21, 0x2D, 0x2D);
        private static readonly Color _colorDarkBg = Color.FromRgb(0x0D, 0x11, 0x17);

        private static readonly SolidColorBrush _brushGray = new(_colorGray);
        private static readonly SolidColorBrush _brushMuted = new(_colorMuted);

        private AppConfig _config;
        private GLMApiService _apiService;
        private bool _isRefreshing;
        private DispatcherTimer _refreshTimer = null!;
        private GLMResponse? _lastResponse;
        private int _lastNotifiedPercentage = -1;
        private bool _dangerNotified;

        public TrayIcon? TrayIcon { get; set; }
        public event EventHandler? TrayReady;

        // 拖拽状态
        private Point _dragStartScreen;
        private bool _isDragging;
        private bool _isPanelOpen;

        // 水位波浪动画
        private int _waterPercentage;
        private double _wavePhase;
        private DispatcherTimer _waveTimer = null!;
        private readonly StreamGeometry _waterGeometry = new();
        private readonly LinearGradientBrush _waterBrush = new() { GradientStops = { new(Colors.Transparent, 0), new(Colors.Transparent, 1) } };

        // 倒计时定时器
        private DispatcherTimer _countdownTimer = null!;
        private TextBlock? _tokenCountdownBlock;
        private TextBlock? _mcpCountdownBlock;
        private TextBlock? _tipTokenCountdownBlock;
        private TextBlock? _tipMcpCountdownBlock;
        private TextBlock? _tipTokenCountdownLabel;
        private TextBlock? _tipMcpCountdownLabel;
        private TextBlock? _tokenCountdownLabel;
        private TextBlock? _mcpCountdownLabel;

        #region Win32 API

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        #endregion

        public MainWindow(AppConfig config)
        {
            _config = config;
            _apiService = new GLMApiService(config.BaseUrl, config.ApiKey);

            InitializeComponent();
            InitializeWindow();
            InitializeTimer();
        }

        private void InitializeWindow()
        {
            Width = _config.BallSize;
            Height = _config.BallSize;

            var cornerRadius = _config.BallSize / 2.0;
            BallBorder.CornerRadius = new CornerRadius(cornerRadius);
            BallBorder.Width = _config.BallSize;
            BallBorder.Height = _config.BallSize;

            PercentText.FontSize = _config.BallSize switch
            {
                >= 90 => 22,
                >= 80 => 20,
                _ => 16
            };

            var colorConv = (PercentageToColorConverter)Resources["PercColorConv"];
            colorConv.WarningThreshold = _config.ThresholdWarning;
            colorConv.DangerThreshold = _config.ThresholdDanger;

            if (_config.Position.X >= 0)
            {
                Left = _config.Position.X;
                Top = _config.Position.Y;
            }
            else
            {
                Left = SystemParameters.PrimaryScreenWidth - _config.BallSize - 20;
                Top = _config.Position.Y;
            }
        }

        private void InitializeTimer()
        {
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_config.RefreshInterval)
            };
            _refreshTimer.Tick += async (_, _) => await RefreshDataAsync();

            _waveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _waveTimer.Tick += (_, _) =>
            {
                _wavePhase += 0.18;
                UpdateWaterFill(_waterPercentage);
            };

            _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _countdownTimer.Tick += (_, _) => UpdateCountdowns();
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, (exStyle | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TrayReady?.Invoke(this, EventArgs.Empty);
            _refreshTimer.Start();
            _waveTimer.Start();
            _countdownTimer.Start();
            _ = RefreshDataAsync();
        }

        private bool _forceExit;

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (!_forceExit)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            SavePosition();
            _refreshTimer.Stop();
            _waveTimer.Stop();
            _countdownTimer.Stop();
            _apiService.Dispose();
        }

        #region 拖拽 (屏幕坐标)

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            _dragStartScreen = PointToScreen(e.GetPosition(this));
            CaptureMouse();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton != MouseButtonState.Pressed) return;

            var currentScreen = PointToScreen(e.GetPosition(this));
            var delta = currentScreen - _dragStartScreen;

            if (!_isDragging)
            {
                if (Math.Abs(delta.X) > 3 || Math.Abs(delta.Y) > 3)
                {
                    _isDragging = true;
                    Cursor = Cursors.SizeAll;
                    BallBorder.Opacity = 0.85;
                }
                return;
            }

            Left += delta.X;
            Top += delta.Y;
            _dragStartScreen = currentScreen;
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();

            if (_isDragging)
            {
                _isDragging = false;
                Cursor = Cursors.Hand;
                BallBorder.Opacity = 1;
                SavePosition();
                return;
            }

            // 没有拖拽 → 当作点击，展开/收起面板
            ToggleDetailPanel();
        }

        private void Ball_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_isDragging) return;

            // 边框高亮
            BallBorderBrush.Color = _colorHover;

            // 三条弧线错开速度旋转，形成流光效果
            StartArcAnimation(HoverArc1, HoverArc1Rotation, 0.9, 0.7, 0.95, 0.8);
            StartArcAnimation(HoverArc2, HoverArc2Rotation, 1.2, 0.6, 0.85, 1.0);
            StartArcAnimation(HoverArc3, HoverArc3Rotation, 1.5, 0.5, 0.75, 1.2);
        }

        private void StartArcAnimation(Ellipse arc, RotateTransform rotation,
            double rotateSec, double startOpacity, double pulseHigh, double pulseSec)
        {
            arc.Opacity = startOpacity;

            var rotAnim = new DoubleAnimation
            {
                From = 0, To = 360,
                Duration = TimeSpan.FromSeconds(rotateSec),
                RepeatBehavior = RepeatBehavior.Forever
            };
            rotation.BeginAnimation(RotateTransform.AngleProperty, rotAnim);

            var pulseAnim = new DoubleAnimation
            {
                From = startOpacity * 0.8, To = pulseHigh,
                Duration = TimeSpan.FromSeconds(pulseSec),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            arc.BeginAnimation(OpacityProperty, pulseAnim);
        }

        private void StopArcAnimation(Ellipse arc, RotateTransform rotation)
        {
            rotation.BeginAnimation(RotateTransform.AngleProperty, null);
            arc.BeginAnimation(OpacityProperty, null);
            arc.Opacity = 0;
        }

        private void Ball_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isDragging) return;

            // 恢复状态色
            var percentage = _lastNotifiedPercentage >= 0 ? _lastNotifiedPercentage : 0;
            var color = percentage >= 80 ? _colorDanger : percentage >= 70 ? _colorWarning : _colorSafe;
            var opacity = percentage >= 80 ? 0.6 : percentage >= 70 ? 0.5 : 0.3;
            BallBorderBrush.Color = color;
            BallGlow.BlurRadius = 16;
            BallGlow.Opacity = opacity;

            StopArcAnimation(HoverArc1, HoverArc1Rotation);
            StopArcAnimation(HoverArc2, HoverArc2Rotation);
            StopArcAnimation(HoverArc3, HoverArc3Rotation);
        }

        #endregion

        #region 详情面板

        private void ToggleDetailPanel()
        {
            _isPanelOpen = !_isPanelOpen;

            if (_isPanelOpen)
            {
                // 关闭 tooltip，避免拦截点击
                BallToolTip.IsOpen = false;

                DetailPanel.Visibility = Visibility.Visible;
                // 仅在内容为空时重建（数据刷新时已构建过）
                if (DetailContent.Children.Count == 0)
                    RebuildDetailContent();

                var anim = new DoubleAnimation
                {
                    From = 20, To = 0,
                    Duration = TimeSpan.FromMilliseconds(100),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                PanelTransform.BeginAnimation(TranslateTransform.YProperty, anim);

                var opacityAnim = new DoubleAnimation
                {
                    From = 0, To = 1,
                    Duration = TimeSpan.FromMilliseconds(100)
                };
                DetailPanel.BeginAnimation(OpacityProperty, opacityAnim);
            }
            else
            {
                var anim = new DoubleAnimation
                {
                    From = 0, To = 20,
                    Duration = TimeSpan.FromMilliseconds(100),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                PanelTransform.BeginAnimation(TranslateTransform.YProperty, anim);

                var opacityAnim = new DoubleAnimation
                {
                    From = 1, To = 0,
                    Duration = TimeSpan.FromMilliseconds(100)
                };
                opacityAnim.Completed += (_, _) =>
                {
                    DetailPanel.Visibility = Visibility.Collapsed;
                    DetailContent.Children.Clear();
                };
                DetailPanel.BeginAnimation(OpacityProperty, opacityAnim);
            }
        }

        #endregion

        #region 数据刷新

        private async Task RefreshDataAsync()
        {
            if (_isRefreshing) return;
            _isRefreshing = true;
            try
            {
                PercentText.Opacity = 0.5;

                var response = await _apiService.FetchUsageAsync();

                if (response?.Data != null)
                {
                    _lastResponse = response;
                    ErrorBadge.Visibility = Visibility.Collapsed;

                    var limits = response.Data.Limits;

                    // 本地计算百分比
                    foreach (var limit in limits ?? Enumerable.Empty<Limit>())
                    {
                        if (limit.CurrentValue.HasValue && limit.Usage.HasValue && limit.Usage.Value > 0)
                            limit.Percentage = (int)Math.Round((double)limit.CurrentValue.Value / limit.Usage.Value * 100);
                    }

                    var tokenLimit = limits?.FirstOrDefault(l => l.Type == "TOKENS_LIMIT");
                    var mcpLimit = limits?.FirstOrDefault(l => l.Type == "TIME_LIMIT");

                    // ========== 悬浮球：以 Token 为主 ==========
                    var percentage = tokenLimit?.Percentage ?? 0;

                    PercentText.Text = $"{percentage}%";
                    PercentText.Foreground = GetColorForPercentage(percentage);

                    // 副文本：只显示倒计时，简洁
                    SubText.Text = tokenLimit?.DisplayCountdown ?? response.Data.Level.ToUpper();

                    UpdateWaterFill(percentage);
                    UpdateBallGlow(percentage);

                    // ========== 详情面板：Token 在上，MCP 工具调用在下 ==========
                    DetailTitle.Text = $"GLM Coding {response.Data.Level.ToUpper()}";
                    BuildDetailPanel(tokenLimit, mcpLimit);

                    // 刷新时间
                    var now = DateTime.Now.ToString("HH:mm:ss");
                    DetailUpdated.Text = $"更新于 {now}";
                    TipUpdated.Text = $"更新于 {now}";
                    if (mcpLimit != null || tokenLimit != null)
                    {
                        TipTitle.Text = $"GLM Coding {response.Data.Level.ToUpper()}";
                        UpdateTooltipLimits(tokenLimit, mcpLimit);
                    }

                    // 危险通知（基于 Token）
                    if (percentage >= _config.ThresholdDanger && _config.ShowNotification)
                    {
                        if (!_dangerNotified)
                        {
                            ShowNotification($"GLM Token 用量警告", $"Token 已达 {percentage}%");
                            _dangerNotified = true;
                        }
                    }
                    else
                    {
                        _dangerNotified = false;
                    }

                    _lastNotifiedPercentage = percentage;
                }
                else
                {
                    if (!string.IsNullOrEmpty(_apiService.LastError) &&
                        (_apiService.LastError.Contains("401") || _apiService.LastError.Contains("403")))
                    {
                        PromptApiKey();
                    }
                    else
                    {
                        ErrorBadge.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                if (IsApiKeyInvalid(ex))
                    PromptApiKey();
                else
                    ErrorBadge.Visibility = Visibility.Visible;
            }
            finally
            {
                _isRefreshing = false;
                PercentText.Opacity = 1;
            }
        }

        private void UpdateTooltipLimits(Limit? token, Limit? mcp)
        {
            TipLimits.Children.Clear();
            _tipTokenCountdownBlock = null;
            _tipMcpCountdownBlock = null;
            _tipTokenCountdownLabel = null;
            _tipMcpCountdownLabel = null;

            if (token != null)
            {
                TipLimits.Children.Add(MakeLabel("Token 限额", _brushGray, 11, true));
                TipLimits.Children.Add(MakeProgressBar(token.Percentage, token.DisplayUsage, GetColorForPercentage(token.Percentage)));
                TipLimits.Children.Add(MakeInfoRow("窗口", token.DisplayWindow, _brushGray, _brushMuted));
                TipLimits.Children.Add(MakeInfoRow("重置时间", token.ResetTimeFull, _brushGray, _brushMuted));
                var countdownRow = MakeCountdownRow(out _tipTokenCountdownLabel, out _tipTokenCountdownBlock, _brushGray, GetColorForPercentage(token.Percentage));
                _tipTokenCountdownBlock.Text = token.DisplayCountdownFull;
                TipLimits.Children.Add(countdownRow);
            }

            if (mcp != null)
            {
                TipLimits.Children.Add(MakeSeparator());
                TipLimits.Children.Add(MakeLabel("MCP 调用", _brushGray, 11, true));
                TipLimits.Children.Add(MakeProgressBar(mcp.Percentage, mcp.DisplayUsage, GetColorForPercentage(mcp.Percentage)));
                TipLimits.Children.Add(MakeInfoRow("窗口", mcp.DisplayWindow, _brushGray, _brushMuted));
                TipLimits.Children.Add(MakeInfoRow("重置时间", mcp.ResetTimeFull, _brushGray, _brushMuted));
                var countdownRow = MakeCountdownRow(out _tipMcpCountdownLabel, out _tipMcpCountdownBlock, _brushGray, GetColorForPercentage(mcp.Percentage));
                _tipMcpCountdownBlock.Text = mcp.DisplayCountdownFull;
                TipLimits.Children.Add(countdownRow);

                if (mcp.UsageDetails?.Count > 0)
                {
                    TipLimits.Children.Add(MakeSeparator());
                    TipLimits.Children.Add(MakeLabel("工具详情", _brushGray, 10, true));
                    foreach (var detail in mcp.UsageDetails)
                    {
                        var usage = detail.Usage ?? detail.CurrentValue ?? 0;
                        TipLimits.Children.Add(MakeInfoRow(detail.DisplayName, $"{usage} 次", _brushGray, _brushMuted));
                    }
                }
            }
        }

        private static bool IsApiKeyInvalid(Exception ex)
        {
            return ex is HttpRequestException httpEx &&
                   (httpEx.StatusCode == HttpStatusCode.Unauthorized ||
                    httpEx.StatusCode == HttpStatusCode.Forbidden);
        }

        private void PromptApiKey()
        {
            ErrorBadge.Visibility = Visibility.Visible;
            PercentText.Text = "!";
            PercentText.Foreground = new SolidColorBrush(Color.FromRgb(0xE7, 0x4C, 0x3C));

            var dialog = new FirstRunWindow();
            dialog.Title = "API Key 无效";
            dialog.Owner = this;
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ApiKey))
            {
                _config.ApiKey = dialog.ApiKey;
                ConfigService.Save(_config);
                _apiService.Dispose();
                _apiService = new GLMApiService(_config.BaseUrl, _config.ApiKey);
                _ = RefreshDataAsync();
            }
        }

        private SolidColorBrush GetColorForPercentage(int percentage)
        {
            if (percentage >= _config.ThresholdDanger) return new SolidColorBrush(_colorDanger);
            if (percentage >= _config.ThresholdWarning) return new SolidColorBrush(_colorWarning);
            return new SolidColorBrush(_colorSafe);
        }

        private void RebuildDetailContent()
        {
            if (_lastResponse?.Data == null) return;
            var limits = _lastResponse.Data.Limits;
            var tokenLimit = limits?.FirstOrDefault(l => l.Type == "TOKENS_LIMIT");
            var mcpLimit = limits?.FirstOrDefault(l => l.Type == "TIME_LIMIT");
            BuildDetailPanel(tokenLimit, mcpLimit);
        }

        private void UpdateCountdowns()
        {
            if (_lastResponse?.Data == null) return;
            var limits = _lastResponse.Data.Limits;
            var tokenLimit = limits?.FirstOrDefault(l => l.Type == "TOKENS_LIMIT");
            var mcpLimit = limits?.FirstOrDefault(l => l.Type == "TIME_LIMIT");

            // 详情面板倒计时
            if (_tokenCountdownBlock != null && tokenLimit != null)
                _tokenCountdownBlock.Text = tokenLimit.DisplayCountdownFull;
            if (_mcpCountdownBlock != null && mcpLimit != null)
                _mcpCountdownBlock.Text = mcpLimit.DisplayCountdownFull;

            // Tooltip 倒计时
            if (_tipTokenCountdownBlock != null && tokenLimit != null)
                _tipTokenCountdownBlock.Text = tokenLimit.DisplayCountdownFull;
            if (_tipMcpCountdownBlock != null && mcpLimit != null)
                _tipMcpCountdownBlock.Text = mcpLimit.DisplayCountdownFull;

            // 悬浮球副文本
            if (tokenLimit != null)
                SubText.Text = tokenLimit.DisplayCountdown;

            // 更新时间
            var now = DateTime.Now.ToString("HH:mm:ss");
            if (DetailUpdated != null)
                DetailUpdated.Text = $"更新于 {now}";
            if (TipUpdated != null)
                TipUpdated.Text = $"更新于 {now}";
        }

        private void BuildDetailPanel(Limit? tokenLimit, Limit? mcpLimit)
        {
            DetailContent.Children.Clear();
            _tokenCountdownBlock = null;
            _mcpCountdownBlock = null;

            if (tokenLimit != null)
            {
                DetailContent.Children.Add(MakeLabel("Token 限额", _brushGray, 12, true));
                DetailContent.Children.Add(MakeProgressBar(tokenLimit.Percentage, tokenLimit.DisplayUsage, GetColorForPercentage(tokenLimit.Percentage)));
                DetailContent.Children.Add(MakeInfoRow("窗口", tokenLimit.DisplayWindow, _brushGray, _brushMuted));
                DetailContent.Children.Add(MakeInfoRow("重置时间", tokenLimit.ResetTimeFull, _brushGray, _brushMuted));
                var countdownRow = MakeCountdownRow(out _tokenCountdownLabel, out _tokenCountdownBlock, _brushGray, GetColorForPercentage(tokenLimit.Percentage));
                _tokenCountdownBlock.Text = tokenLimit.DisplayCountdownFull;
                DetailContent.Children.Add(countdownRow);
            }

            if (mcpLimit != null)
            {
                DetailContent.Children.Add(MakeSeparator());
                DetailContent.Children.Add(MakeLabel("MCP 调用", _brushGray, 12, true));
                DetailContent.Children.Add(MakeProgressBar(mcpLimit.Percentage, mcpLimit.DisplayUsage, GetColorForPercentage(mcpLimit.Percentage)));
                DetailContent.Children.Add(MakeInfoRow("窗口", mcpLimit.DisplayWindow, _brushGray, _brushMuted));
                DetailContent.Children.Add(MakeInfoRow("重置时间", mcpLimit.ResetTimeFull, _brushGray, _brushMuted));
                var countdownRow = MakeCountdownRow(out _mcpCountdownLabel, out _mcpCountdownBlock, _brushGray, GetColorForPercentage(mcpLimit.Percentage));
                _mcpCountdownBlock.Text = mcpLimit.DisplayCountdownFull;
                DetailContent.Children.Add(countdownRow);

                if (mcpLimit.UsageDetails?.Count > 0)
                {
                    DetailContent.Children.Add(MakeSeparator());
                    DetailContent.Children.Add(MakeLabel("工具详情", _brushGray, 11, true));
                    foreach (var detail in mcpLimit.UsageDetails)
                    {
                        var usage = detail.Usage ?? detail.CurrentValue ?? 0;
                        DetailContent.Children.Add(MakeInfoRow(detail.DisplayName, $"{usage} 次", _brushGray, _brushMuted));
                    }
                }
            }
        }

        private static TextBlock MakeLabel(string text, Brush foreground, double fontSize, bool bold)
        {
            return new TextBlock
            {
                Text = text, FontSize = fontSize, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = foreground, Margin = new Thickness(0, 0, 0, 6)
            };
        }

        private static Grid MakeProgressBar(int percentage, string displayUsage, Brush fillBrush)
        {
            var grid = new Grid { Height = 24, ClipToBounds = true, Margin = new Thickness(0, 0, 0, 8) };
            grid.Children.Add(new Border { CornerRadius = new CornerRadius(6), Background = new SolidColorBrush(_colorDarkBg) });

            var fill = new Border
            {
                CornerRadius = new CornerRadius(6), ClipToBounds = true,
                Background = fillBrush,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            grid.Children.Add(fill);

            var txt = new TextBlock
            {
                Text = displayUsage, FontSize = 10, FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            // 进度条文字自带对比度，不需要额外阴影
            grid.Children.Add(txt);

            grid.Loaded += (_, _) =>
            {
                var w = grid.ActualWidth;
                if (w > 0)
                    fill.Width = w * Math.Min(percentage, 100) / 100.0;
            };

            return grid;
        }

        private static DockPanel MakeInfoRow(string label, string value, Brush labelBrush, Brush valueBrush)
        {
            var panel = new DockPanel { Margin = new Thickness(0, 2, 0, 2) };
            if (!string.IsNullOrEmpty(label))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = label, FontSize = 12, Foreground = labelBrush,
                    VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0)
                });
            }
            panel.Children.Add(new TextBlock
            {
                Text = value, FontSize = 12, Foreground = valueBrush,
                HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center
            });
            return panel;
        }

        private static DockPanel MakeCountdownRow(out TextBlock labelBlock, out TextBlock valueBlock, Brush labelBrush, Brush valueBrush)
        {
            var panel = new DockPanel { Margin = new Thickness(0, 2, 0, 4) };
            labelBlock = new TextBlock
            {
                Text = "剩余", FontSize = 12, Foreground = labelBrush,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0)
            };
            valueBlock = new TextBlock
            {
                Text = "--", FontSize = 12, FontWeight = FontWeights.SemiBold,
                Foreground = valueBrush,
                HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center
            };
            panel.Children.Add(labelBlock);
            panel.Children.Add(valueBlock);
            return panel;
        }

        private static Border MakeSeparator()
        {
            return new Border
            {
                Height = 1, Background = new SolidColorBrush(_colorSeparator),
                Margin = new Thickness(0, 6, 0, 6)
            };
        }

        private void UpdateBallGlow(int percentage)
        {
            var color = percentage >= 80 ? _colorDanger : percentage >= 70 ? _colorWarning : _colorSafe;
            var opacity = percentage >= 80 ? 0.6 : percentage >= 70 ? 0.5 : 0.3;

            BallGlow.Color = color;
            BallGlow.Opacity = opacity;
            BallBorderBrush.Color = color;
        }

        private void UpdateWaterFill(int percentage)
        {
            _waterPercentage = percentage;
            const double r = 43;
            const double d = r * 2;

            var color = percentage >= 80 ? _colorDanger : percentage >= 70 ? _colorWarning : _colorSafe;

            if (percentage <= 0)
            {
                WaterFill.Data = null;
                return;
            }

            if (percentage >= 100)
            {
                _waterBrush.StartPoint = new Point(0, 0);
                _waterBrush.EndPoint = new Point(0, d);
                _waterBrush.GradientStops[0].Color = Color.FromArgb(60, color.R, color.G, color.B);
                _waterBrush.GradientStops[1].Color = Color.FromArgb(140, color.R, color.G, color.B);
                WaterFill.Fill = _waterBrush;
                WaterFill.Data = new EllipseGeometry(new Rect(0, 0, d, d));
                return;
            }

            // 水位线 y 坐标（从顶部算起）
            double yLevel = d * (1.0 - percentage / 100.0);
            double dy = yLevel - r;
            double dx = Math.Sqrt(Math.Max(0, r * r - dy * dy));
            double leftX = r - dx;
            double rightX = r + dx;

            // 使用 StreamGeometry 避免每帧分配 PathFigure/LineSegment/ArcSegment
            using var ctx = _waterGeometry.Open();
            double startY = yLevel + 4 * Math.Sin(_wavePhase);
            ctx.BeginFigure(new Point(leftX, startY), true, true);

            const int segments = 12;
            double chordWidth = rightX - leftX;
            for (int i = 1; i <= segments; i++)
            {
                double t = (double)i / segments;
                double x = leftX + chordWidth * t;
                double waveY = yLevel + 4 * Math.Sin(2 * Math.PI * (t * 2 + _wavePhase / (2 * Math.PI)));
                ctx.LineTo(new Point(x, waveY), true, false);
            }

            ctx.ArcTo(new Point(leftX, startY), new Size(r, r), 0,
                percentage > 50, SweepDirection.Clockwise, true, false);

            // 更新渐变
            _waterBrush.StartPoint = new Point(0, yLevel - 4);
            _waterBrush.EndPoint = new Point(0, d);
            _waterBrush.GradientStops[0].Color = Color.FromArgb(50, color.R, color.G, color.B);
            _waterBrush.GradientStops[1].Color = Color.FromArgb(130, color.R, color.G, color.B);
            WaterFill.Fill = _waterBrush;
            WaterFill.Data = _waterGeometry;
        }

        private void ShowNotification(string title, string message)
        {
            try
            {
                TrayIcon?.ShowBalloonTip(title, message);
            }
            catch { }
        }

        #endregion

        #region 菜单事件

        public void ShowTrayMenu()
        {
            var darkBg = new SolidColorBrush(Color.FromRgb(0x1C, 0x23, 0x33));
            var borderBrush = new SolidColorBrush(Color.FromRgb(0x30, 0x36, 0x3D));
            var textFg = new SolidColorBrush(Color.FromRgb(0xE6, 0xED, 0xF3));

            var menu = new ContextMenu
            {
                Background = darkBg,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                Foreground = textFg
            };

            menu.Items.Add(CreateTrayMenuItem("显示/隐藏悬浮球", "\xE8B7", _brushGray, (_, _) => ToggleVisibility(), textFg));
            menu.Items.Add(CreateTrayMenuItem("设置", "\xE713", _brushGray, (_, _) => OpenSettings(), textFg));

            var sep = new Separator
            {
                Background = new SolidColorBrush(_colorSeparator),
                Margin = new Thickness(4, 2, 4, 2)
            };
            menu.Items.Add(sep);

            menu.Items.Add(CreateTrayMenuItem("退出", "\xE711", new SolidColorBrush(_colorDanger), (_, _) => ForceExit(), textFg));

            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            menu.IsOpen = true;
        }

        private static MenuItem CreateTrayMenuItem(string text, string iconGlyph, Brush iconBrush, RoutedEventHandler handler, Brush textFg)
        {
            var item = new MenuItem
            {
                FontWeight = FontWeights.Normal,
                Foreground = textFg,
                Background = Brushes.Transparent,
                Padding = new Thickness(8, 6, 8, 6)
            };
            item.Header = new StackPanel { Orientation = Orientation.Horizontal, Children =
            {
                new TextBlock { Text = iconGlyph, FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 13, Foreground = iconBrush,
                    Margin = new Thickness(0,0,8,0), VerticalAlignment = VerticalAlignment.Center },
                new TextBlock { Text = text, Foreground = textFg, VerticalAlignment = VerticalAlignment.Center }
            }};
            item.Click += handler;
            return item;
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await RefreshDataAsync();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(_config);
            settingsWindow.Owner = this;
            if (settingsWindow.ShowDialog() == true)
            {
                _config = settingsWindow.UpdatedConfig;
                ConfigService.Save(_config);
                ApplyConfigChanges();
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            ForceExit();
        }

        public void ToggleVisibility()
        {
            if (IsVisible)
                Hide();
            else
                Show();
        }

        public void OpenDetailPanel()
        {
            if (!IsVisible) Show();
            if (!_isPanelOpen)
                ToggleDetailPanel();
        }

        public void OpenSettings()
        {
            if (!IsVisible) Show();
            Settings_Click(this, new RoutedEventArgs());
        }

        public void ForceExit()
        {
            _forceExit = true;
            Application.Current.Shutdown();
        }

        private void ApplyConfigChanges()
        {
            _apiService.Dispose();
            _apiService = new GLMApiService(_config.BaseUrl, _config.ApiKey);

            _refreshTimer.Interval = TimeSpan.FromSeconds(_config.RefreshInterval);

            var colorConv = (PercentageToColorConverter)Resources["PercColorConv"];
            colorConv.WarningThreshold = _config.ThresholdWarning;
            colorConv.DangerThreshold = _config.ThresholdDanger;

            _ = RefreshDataAsync();
        }

        #endregion

        #region 位置管理

        private DispatcherTimer? _savePositionTimer;

        private void SavePosition()
        {
            _config.Position.X = Left;
            _config.Position.Y = Top;
            _savePositionTimer?.Stop();
            _savePositionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _savePositionTimer.Tick += (_, _) =>
            {
                _savePositionTimer.Stop();
                ConfigService.Save(_config);
            };
            _savePositionTimer.Start();
        }

        #endregion
    }
}
