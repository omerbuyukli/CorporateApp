using CorporateApp.Application.Interfaces;
using CorporateApp.Core.Entities.DiaEntities;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CorporateApp.Infrastructure.Services.DiaServices
{
    public class DiaLoginService : IDiaLoginService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DiaLoginService> _logger;

        public DiaLoginService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<DiaLoginService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(bool Success, string SessionId, string Message)> LoginAsync()
        {
            try
            {
                var account = new Account
                {
                    Login = new Login
                    {
                        Username = _configuration["DiaApi:Username"],
                        Password = _configuration["DiaApi:Password"],
                        Params = new Params
                        {
                            ApiKey = _configuration["DiaApi:ApiKey"]
                        },
                        Lang = "tr",
                        DisconnectSameUser = "True"
                    }
                };

                var client = _httpClientFactory.CreateClient("DiaApi");
                var json = JsonSerializer.Serialize(account);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync("login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<DynamicResponse>(responseContent);

                    return (true, result.SessionId, result.Message);
                }

                return (false, null, "Login failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dia login error");
                return (false, null, ex.Message);
            }
        }
    }
}
