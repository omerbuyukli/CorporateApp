using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace CorporateApp.Infrastructure.Filters
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        private readonly IConfiguration _configuration;

        public HangfireAuthorizationFilter(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // Development ortamında direkt izin ver
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (env == "Development")
            {
                return true; // Development'ta herkese açık
            }

            // Production ortamında authentication kontrolü
            if (httpContext.User.Identity?.IsAuthenticated != true)
            {
                return false; // Giriş yapmamışsa izin verme
            }

            // Admin rolü kontrolü (Production için)
            var allowedRoles = _configuration.GetSection("Hangfire:AllowedRoles").Get<string[]>() ?? new[] { "Admin" };

            foreach (var role in allowedRoles)
            {
                if (httpContext.User.IsInRole(role))
                {
                    return true; // İzin verilen rollerden birine sahipse
                }
            }

            return false; // Hiçbir koşul sağlanmadıysa izin verme
        }
    }
}
