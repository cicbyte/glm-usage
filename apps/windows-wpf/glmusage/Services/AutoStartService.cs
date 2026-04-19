using System.IO;
using Microsoft.Win32;

namespace glmusage.Services
{
    public static class AutoStartService
    {
        private const string AppName = "GLMFloatBall";
        private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public static bool IsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool SetEnabled(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
                if (key == null) return false;

                if (enabled)
                {
                    var exePath = Environment.ProcessPath;
                    if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath)) return false;
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AutoStart] Failed: {ex.Message}");
                return false;
            }
        }
    }
}
