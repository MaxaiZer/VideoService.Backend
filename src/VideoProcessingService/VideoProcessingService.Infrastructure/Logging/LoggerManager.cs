using NLog;
using VideoProcessingService.Core.Interfaces;

namespace VideoProcessingService.Infrastructure.Logging
{
    public class LoggerManager : ILoggerManager
    {
        private static readonly NLog.ILogger _logger = LogManager.GetCurrentClassLogger();
        
        public void LogDebug(string message) => _logger.Debug(message);
        public void LogError(string message) => _logger.Error(message);
        public void LogInfo(string message) => _logger.Info(message);
        public void LogWarn(string message) => _logger.Warn(message);
    }

}
