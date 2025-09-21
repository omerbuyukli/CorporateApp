using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CorporateApp.Core.Interfaces;
using CorporateApp.Infrastructure.Data;
using CorporateApp.Infrastructure.Logging;
using CorporateApp.Infrastructure.Services;
using CorporateApp.Application.Interfaces;
using CorporateApp.Application.Services;
using CorporateApp.Application.Services;

namespace CorporateApp.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Database - SQL Server kullan (SQLite DEĞİL!)
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Repositories
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUserRepository, UserRepository>();

            // Infrastructure Services
            services.AddSingleton<IAppLogger, LoggerService>();
            services.AddScoped<ITokenService, TokenService>();

            // Application Services
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthService, AuthService>();

            services.AddScoped<IErpSyncJobService, ErpSyncJobService>();

            return services;
        }
    }
}