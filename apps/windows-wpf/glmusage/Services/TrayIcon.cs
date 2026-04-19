using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace glmusage.Services
{
    public class TrayIcon : IDisposable
    {
        #region Win32 API

        private const int NIM_ADD = 0x00000000;
        private const int NIM_MODIFY = 0x00000001;
        private const int NIM_DELETE = 0x00000002;
        private const int NIF_MESSAGE = 0x00000001;
        private const int NIF_ICON = 0x00000002;
        private const int NIF_TIP = 0x00000004;
        private const int NIF_INFO = 0x00000010;
        private const int NIIF_WARNING = 0x00000002;

        private const int WM_TRAYICON = 0x8000;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_RBUTTONUP = 0x0205;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NOTIFYICONDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public uint dwState;
            public uint dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public uint uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public uint dwInfoFlags;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA data);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        #endregion

        private IntPtr _hwnd;
        private IntPtr _iconHandle;
        private HwndSource? _hwndSource;
        private bool _disposed;

        public event Action? LeftClick;
        public event Action? DoubleClick;
        public event Action? RightClick;

        public void Create(Window window, string toolTip)
        {
            _hwndSource = (HwndSource)PresentationSource.FromVisual(window);
            _hwnd = _hwndSource.Handle;
            _iconHandle = GenerateIcon().Handle;

            _hwndSource.AddHook(WndProc);

            var data = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hwnd,
                uID = 1,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = WM_TRAYICON,
                hIcon = _iconHandle,
                szTip = toolTip.Length > 127 ? toolTip[..127] : toolTip
            };
            Shell_NotifyIcon(NIM_ADD, ref data);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_TRAYICON)
            {
                var mouseMsg = (int)lParam & 0xFFFF;
                switch (mouseMsg)
                {
                    case WM_LBUTTONUP:
                        LeftClick?.Invoke();
                        break;
                    case WM_LBUTTONDBLCLK:
                        DoubleClick?.Invoke();
                        break;
                    case WM_RBUTTONUP:
                        RightClick?.Invoke();
                        break;
                }
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void ShowBalloonTip(string title, string message)
        {
            var data = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hwnd,
                uID = 1,
                uFlags = NIF_INFO,
                szInfo = message.Length > 255 ? message[..255] : message,
                szInfoTitle = title.Length > 63 ? title[..63] : title,
                dwInfoFlags = NIIF_WARNING,
                uTimeoutOrVersion = 3000
            };
            Shell_NotifyIcon(NIM_MODIFY, ref data);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            var data = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hwnd,
                uID = 1
            };
            Shell_NotifyIcon(NIM_DELETE, ref data);

            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(WndProc);
                _hwndSource.Dispose();
                _hwndSource = null;
            }

            if (_iconHandle != IntPtr.Zero)
                DestroyIcon(_iconHandle);
        }

        private static Icon GenerateIcon()
        {
            const int size = 32;
            using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            // 绿色圆形
            using var brush = new SolidBrush(Color.FromArgb(255, 0x2E, 0xCC, 0x71));
            g.FillEllipse(brush, 2, 2, size - 4, size - 4);

            // 白色 G
            using var font = new Font("Segoe UI", 16, System.Drawing.FontStyle.Bold);
            using var textBrush = new SolidBrush(Color.White);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("G", font, textBrush, new RectangleF(0, 0, size, size), sf);

            // 从 Bitmap 的 HICON 创建 Icon（v8.0 仍支持）
            var hIcon = bmp.GetHicon();
            return Icon.FromHandle(hIcon);
        }
    }
}
