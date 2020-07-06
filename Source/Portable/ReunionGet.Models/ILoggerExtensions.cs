using Microsoft.Extensions.Logging;

namespace ReunionGet.Models
{
    public static class ILoggerExtensions
    {
        public static ILogger? OnEnabled(this ILogger? logger, LogLevel logLevel)
        {
            if (logger is null)
                return null;
            if (!logger.IsEnabled(logLevel))
                return null;
            return logger;
        }
    }
}
