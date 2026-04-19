using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using glmusage.Models;

namespace glmusage.Services
{
    public static class ConfigService
    {
        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GLMFloatBall");

        private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                    config.ApiKey = DecryptApiKey(config.ApiKey);
                    return config;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Config] Load failed: {ex.Message}");
            }

            return new AppConfig();
        }

        public static void Save(AppConfig config)
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                var clone = new AppConfig
                {
                    ApiKey = EncryptApiKey(config.ApiKey),
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
                var json = JsonSerializer.Serialize(clone, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Config] Save failed: {ex.Message}");
            }
        }

        private static readonly byte[] Entropy = { 0x47, 0x4C, 0x4D, 0x55, 0x73, 0x61, 0x67, 0x65 };

        private static string EncryptApiKey(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            try
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(plainText);
                var encrypted = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encrypted);
            }
            catch
            {
                return plainText;
            }
        }

        private static string DecryptApiKey(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;
            try
            {
                var bytes = Convert.FromBase64String(cipherText);
                var decrypted = ProtectedData.Unprotect(bytes, Entropy, DataProtectionScope.CurrentUser);
                return System.Text.Encoding.UTF8.GetString(decrypted);
            }
            catch
            {
                return cipherText;
            }
        }
    }
}
