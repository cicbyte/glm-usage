using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using glmusage.Models;
using glmusage.Services;

namespace glmusage
{
    public partial class SettingsWindow : Window
    {
        private readonly AppConfig _originalConfig;
        public AppConfig UpdatedConfig { get; private set; }
        private bool _keyVisible;

        public SettingsWindow(AppConfig config)
        {
            _originalConfig = config;
            UpdatedConfig = new AppConfig
            {
                ApiKey = config.ApiKey,
                BaseUrl = config.BaseUrl,
                RefreshInterval = config.RefreshInterval,
                BallSize = config.BallSize,
                BallShape = config.BallShape,
                Position = config.Position,
                Theme = config.Theme,
                ThresholdWarning = config.ThresholdWarning,
                ThresholdDanger = config.ThresholdDanger,
                AutoStart = config.AutoStart,
                ShowNotification = config.ShowNotification
            };

            InitializeComponent();
            LoadConfig();
        }

        private void LoadConfig()
        {
            SetApiKeyText(_originalConfig.ApiKey);
            _keyVisible = false;
            BaseUrlBox.Text = _originalConfig.BaseUrl;
            RefreshBox.Text = _originalConfig.RefreshInterval.ToString();
            AutoStartBox.IsChecked = _originalConfig.AutoStart;
            NotifBox.IsChecked = _originalConfig.ShowNotification;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(RefreshBox.Text, out var interval) || interval < 30)
            {
                MessageBox.Show("刷新间隔不能小于30秒", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            UpdatedConfig.ApiKey = _keyVisible ? ApiKeyBox.Text.Trim() : _originalConfig.ApiKey;
            UpdatedConfig.BaseUrl = BaseUrlBox.Text.Trim();
            UpdatedConfig.RefreshInterval = interval;
            UpdatedConfig.AutoStart = AutoStartBox.IsChecked == true;
            UpdatedConfig.ShowNotification = NotifBox.IsChecked == true;

            AutoStartService.SetEnabled(UpdatedConfig.AutoStart);
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void ToggleKeyVis_Click(object sender, RoutedEventArgs e)
        {
            _keyVisible = !_keyVisible;
            SetApiKeyText(_keyVisible ? UpdatedConfig.ApiKey : _originalConfig.ApiKey);

            var btn = (Button)sender;
            btn.ApplyTemplate();
            var icon = (TextBlock)btn.Template.FindName("EyeIcon", btn);
            if (icon != null)
                icon.Text = _keyVisible ? "隐藏" : "显示";
        }

        private void SetApiKeyText(string text)
        {
            ApiKeyBox.Text = string.IsNullOrEmpty(text) ? string.Empty : (_keyVisible ? text : new string('●', text.Length));
        }

        private void SpinUp_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(RefreshBox.Text, out var val))
                RefreshBox.Text = Math.Min(val + 30, 3600).ToString();
        }

        private void SpinDown_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(RefreshBox.Text, out var val))
                RefreshBox.Text = Math.Max(val - 30, 30).ToString();
        }
    }
}
