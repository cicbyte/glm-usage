using System.Text.Json.Serialization;

namespace glmusage.Models
{
    public class AppConfig
    {
        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; } = string.Empty;

        [JsonPropertyName("baseUrl")]
        public string BaseUrl { get; set; } = "https://open.bigmodel.cn";

        [JsonPropertyName("refreshInterval")]
        public int RefreshInterval { get; set; } = 300;

        [JsonPropertyName("ballSize")]
        public int BallSize { get; set; } = 80;

        [JsonPropertyName("ballShape")]
        public string BallShape { get; set; } = "circle";

        [JsonPropertyName("position")]
        public PositionConfig Position { get; set; } = new();

        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "dark";

        [JsonPropertyName("thresholdWarning")]
        public int ThresholdWarning { get; set; } = 70;

        [JsonPropertyName("thresholdDanger")]
        public int ThresholdDanger { get; set; } = 80;

        [JsonPropertyName("autoStart")]
        public bool AutoStart { get; set; } = true;

        [JsonPropertyName("showNotification")]
        public bool ShowNotification { get; set; } = true;
    }

    public class PositionConfig
    {
        [JsonPropertyName("x")]
        public double X { get; set; } = -1;

        [JsonPropertyName("y")]
        public double Y { get; set; } = 100;
    }
}
