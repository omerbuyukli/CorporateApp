using Microsoft.Extensions.Options;
using CorporateApp.Infrastructure.Configuration;
using CorporateApp.Infrastructure.Constants;
using Microsoft.Extensions.Logging;
using CorporateApp.Application.Interfaces;

namespace CorporateApp.Infrastructure.Services.DiaServices
{
    public class DiaLoginService : DiaApiServiceBase, IDiaLoginService
    {
        public DiaLoginService(
            IHttpClientFactory httpClientFactory,
            IOptions<DiaApiConfiguration> configuration,
            ILogger<DiaLoginService> logger)
            : base(httpClientFactory, configuration, logger)
        {
        }

        public async Task<(bool Success, string SessionId, string Message)> LoginAsync()
        {
            try
            {
                var loginRequest = new
                {
                    login = new
                    {
                        username = _configuration.Username,
                        password = _configuration.Password,
                        @params = new
                        {
                            apikey = _configuration.ApiKey
                        },
                        disconnect_same_user = "True"
                    }
                };

                // Constants'tan module adını al, Configuration'dan endpoint'i al
                var response = await SendRequestAsync<dynamic>(
                    DiaApiConstants.Modules.SIS,
                    loginRequest
                );

                if (response != null)
                {
                    string code = response.code?.ToString();
                    string msg = response.msg?.ToString();

                    if (code == DiaApiConstants.ResponseCodes.Success)
                    {
                        SetSessionId(msg);
                        return (true, msg, "Login successful");
                    }
                    else if (code == DiaApiConstants.ResponseCodes.Unauthorized)
                    {
                        return (false, null, $"Login failed: {msg}");
                    }

                    return (false, null, $"Unexpected response: {msg}");
                }

                return (false, null, "No response from server");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed");
                return (false, null, ex.Message);
            }
        }
    }
}