using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using CorporateApp.Infrastructure.Configuration;
using CorporateApp.Infrastructure.Constants;
using Microsoft.Extensions.Logging;

namespace CorporateApp.Infrastructure.Services.DiaServices
{
    public abstract class DiaApiServiceBase
    {
        protected readonly IHttpClientFactory _httpClientFactory;
        protected readonly DiaApiConfiguration _configuration;
        protected readonly ILogger _logger;
        private string? _sessionId;

        protected DiaApiServiceBase(
            IHttpClientFactory httpClientFactory,
            IOptions<DiaApiConfiguration> configuration,
            ILogger logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration.Value;
            _logger = logger;
        }

        protected string GetEndpoint(string module)
        {
            if (_configuration.Endpoints.TryGetValue(module, out var endpoint))
            {
                return endpoint;
            }

            throw new InvalidOperationException($"Endpoint for module '{module}' not found in configuration");
        }

        protected async Task<T?> SendRequestAsync<T>(string module, object request, string? sessionId = null)
        {
            try
            {
                var endpoint = GetEndpoint(module);
                var client = _httpClientFactory.CreateClient("DiaApi");

                // Request'i JSON string'e çevir
                var json = JsonConvert.SerializeObject(request);

                _logger.LogDebug($"Sending request to {endpoint}: {json}");

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync(endpoint, content);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug($"Response from {endpoint}: {responseContent}");

                if (!string.IsNullOrEmpty(responseContent))
                {
                    // Hata kontrolü
                    if (responseContent.Contains("faultcode"))
                    {
                        _logger.LogError($"API Error: {responseContent}");
                        return default;
                    }

                    return JsonConvert.DeserializeObject<T>(responseContent);
                }

                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending request to module {module}");
                throw;
            }
        }
        protected void SetSessionId(string sessionId)
        {
            _sessionId = sessionId;
        }

        protected string? GetSessionId() => _sessionId;
    }
}