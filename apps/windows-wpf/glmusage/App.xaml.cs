using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

using glmusage.Models;
using glmusage.Services;

[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]

namespace glmusage
{
    public partial class App : Application
    {
        private static Mutex? _mutex;
        private MainWindow? _mainWindow;
        private TrayIcon? _trayIcon;
        private DispatcherTimer? _clickTimer;
        private bool _doubleClicked;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // 单实例检测
            if (!EnsureSingleInstance())
            {
                MessageBox.Show("GLM Usage 已在运行", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            // 加载配置
            var config = ConfigService.Load();

            // 首次运行：检查是否有 API Key
            if (string.IsNullOrWhiteSpace(config.ApiKey))
            {
                var firstRun = new FirstRunWindow();
                if (firstRun.ShowDialog() != true || string.IsNullOrWhiteSpace(firstRun.ApiKey))
                {
                    Shutdown();
                    return;
                }
                config.ApiKey = firstRun.ApiKey;
                ConfigService.Save(config);
            }

            // 创建主窗口
            _mainWindow = new MainWindow(config);
            _mainWindow.TrayReady += OnTrayReady!;
            _mainWindow.Show();
        }

        private void OnTrayReady(object sender, EventArgs e)
        {
            _trayIcon = new TrayIcon();
            _trayIcon.Create(_mainWindow!, "GLM Usage");
            _trayIcon.LeftClick += OnTrayLeftClick;
            _trayIcon.DoubleClick += OnTrayDoubleClick;
            _trayIcon.RightClick += () => _mainWindow!.ShowTrayMenu();
            _mainWindow!.TrayIcon = _trayIcon;
        }

        private void OnTrayLeftClick()
        {
            _doubleClicked = false;
            _clickTimer?.Stop();
            _clickTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _clickTimer.Tick += (_, _) =>
            {
                _clickTimer.Stop();
                if (!_doubleClicked)
                    _mainWindow!.ToggleVisibility();
            };
            _clickTimer.Start();
        }

        private void OnTrayDoubleClick()
        {
            _doubleClicked = true;
            _clickTimer?.Stop();
            _mainWindow!.Show();
            _mainWindow.OpenDetailPanel();
        }

        private static bool EnsureSingleInstance()
        {
            _mutex = new Mutex(true, "GLMFloatBall_SingleInstance", out var createdNew);
            return createdNew;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayIcon?.Dispose();
            try { _mutex?.ReleaseMutex(); } catch { }
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }
}
