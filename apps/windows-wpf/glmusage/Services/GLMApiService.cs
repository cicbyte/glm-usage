using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using glmusage.Models;

namespace glmusage.Services
{
    public class GLMApiService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public string? LastError { get; private set; }

        public GLMApiService(string baseUrl, string apiKey)
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<GLMResponse?> FetchUsageAsync()
        {
            LastError = null;
            try
            {
                var url = $"{_baseUrl}/api/monitor/usage/quota/limit";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<GLMResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (HttpRequestException ex)
            {
                LastError = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[API] HTTP error: {ex.StatusCode} {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[API] Error: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        public void UpdateApiKey(string apiKey)
        {
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
