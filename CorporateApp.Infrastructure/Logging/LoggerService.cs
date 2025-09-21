using CorporateApp.Core.Interfaces;
using Serilog;

namespace CorporateApp.Infrastructure.Logging
{
    public class LoggerService : IAppLogger
    {
        public void LogInformation(string message, params object[] args)
        {
            Log.Information(message, args);
        }

        public void LogWarning(string message, params object[] args)
        {
            Log.Warning(message, args);
        }

        public void LogError(string message, params object[] args)
        {
            Log.Error(message, args);
        }
    }
}
