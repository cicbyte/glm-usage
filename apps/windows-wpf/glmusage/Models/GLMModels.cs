using System.Text.Json.Serialization;

namespace glmusage.Models
{
    public class GLMResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("msg")]
        public string Msg { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public GLMData? Data { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }

    public class GLMData
    {
        [JsonPropertyName("level")]
        public string Level { get; set; } = string.Empty;

        [JsonPropertyName("limits")]
        public List<Limit>? Limits { get; set; }
    }

    public class Limit
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("unit")]
        public int? Unit { get; set; }

        [JsonPropertyName("number")]
        public int? Number { get; set; }

        [JsonPropertyName("usage")]
        public int? Usage { get; set; }

        [JsonPropertyName("currentValue")]
        public int? CurrentValue { get; set; }

        [JsonPropertyName("remaining")]
        public int? Remaining { get; set; }

        [JsonPropertyName("percentage")]
        public int Percentage { get; set; }

        [JsonPropertyName("nextResetTime")]
        public long? NextResetTime { get; set; }

        [JsonPropertyName("usageDetails")]
        public List<UsageDetail>? UsageDetails { get; set; }

        [JsonIgnore]
        public string DisplayName => Type switch
        {
            "TIME_LIMIT" => "MCP 调用",
            "TOKENS_LIMIT" => "Token 限额",
            _ => Type
        };

        [JsonIgnore]
        public string DisplayUsage => CurrentValue.HasValue && Usage.HasValue
            ? $"{CurrentValue.Value} / {Usage.Value}"
            : $"{Percentage}%";

        [JsonIgnore]
        public string DisplayRemaining => Remaining.HasValue
            ? $"剩余 {Remaining.Value}"
            : "";

        [JsonIgnore]
        public string DisplayWindow => Type switch
        {
            "TIME_LIMIT" when Unit.HasValue => $"{Unit.Value}h 窗口",
            "TOKENS_LIMIT" when Number.HasValue => $"{Number.Value}h 窗口",
            _ => ""
        };

        [JsonIgnore]
        public string DisplayCountdown
        {
            get
            {
                if (!NextResetTime.HasValue) return "--";
                var reset = DateTimeOffset.FromUnixTimeMilliseconds(NextResetTime.Value).ToLocalTime();
                var remaining = reset - DateTimeOffset.Now;
                if (remaining <= TimeSpan.Zero) return "即将重置";
                if (remaining.TotalDays >= 1)
                    return $"{(int)remaining.TotalDays}d {(int)remaining.Hours}h {(int)remaining.Minutes % 60}m";
                if (remaining.TotalHours >= 1)
                    return $"{(int)remaining.TotalHours}h {(int)remaining.Minutes % 60}m";
                return $"{(int)remaining.TotalMinutes}m";
            }
        }

        [JsonIgnore]
        public string DisplayCountdownFull
        {
            get
            {
                if (!NextResetTime.HasValue) return "--";
                var reset = DateTimeOffset.FromUnixTimeMilliseconds(NextResetTime.Value).ToLocalTime();
                var remaining = reset - DateTimeOffset.Now;
                if (remaining <= TimeSpan.Zero) return "即将重置";
                if (remaining.TotalDays >= 1)
                    return $"{(int)remaining.TotalDays}d {(int)remaining.Hours}h {(int)remaining.Minutes % 60}m {(int)remaining.Seconds % 60}s";
                return $"{(int)remaining.TotalHours}h {(int)remaining.Minutes % 60}m {(int)remaining.Seconds % 60}s";
            }
        }

        [JsonIgnore]
        public string ResetTimeFull => NextResetTime.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(NextResetTime.Value)
                .ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
            : "--";
    }

    public class UsageDetail
    {
        [JsonPropertyName("modelCode")]
        public string ModelCode { get; set; } = string.Empty;

        [JsonPropertyName("modelId")]
        public string? ModelId { get; set; }

        [JsonPropertyName("usage")]
        public int? Usage { get; set; }

        [JsonPropertyName("currentValue")]
        public int? CurrentValue { get; set; }

        [JsonIgnore]
        public string DisplayName => ModelCode switch
        {
            "search-prime" => "Web Search",
            "web-reader" => "Web Reader",
            "zread" => "Repo Read",
            "4_5v-mcp" => "4.5V Vision",
            _ => ModelCode
        };
    }
}
