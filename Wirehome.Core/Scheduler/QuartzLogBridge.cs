using Microsoft.Extensions.Logging;
using System;

namespace Wirehome.Core.Scheduler
{
    public class QuartzLogBridge
    {
        private readonly ILogger _logger;

        public QuartzLogBridge(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        //public Logger GetLogger(string name)
        //{
        //    return (level, messageProvider, exception, parameters) =>
        //    {
        //        if (messageProvider == null)
        //        {
        //            return true;
        //        }

        //        Microsoft.Extensions.Logging.LogLevel logLevel;
        //        switch (level)
        //        {
        //            case Quartz.Logging.LogLevel.Info:
        //                {
        //                    logLevel = Microsoft.Extensions.Logging.LogLevel.Information;
        //                    break;
        //                }
        //            case Quartz.Logging.LogLevel.Warn:
        //                {
        //                    logLevel = Microsoft.Extensions.Logging.LogLevel.Warning;
        //                    break;
        //                }
        //            case Quartz.Logging.LogLevel.Error:
        //                {
        //                    logLevel = Microsoft.Extensions.Logging.LogLevel.Error;
        //                    break;
        //                }
        //            case Quartz.Logging.LogLevel.Fatal:
        //                {
        //                    logLevel = Microsoft.Extensions.Logging.LogLevel.Critical;
        //                    break;
        //                }
        //            case Quartz.Logging.LogLevel.Trace:
        //                {
        //                    logLevel = Microsoft.Extensions.Logging.LogLevel.Trace;
        //                    break;
        //                }

        //            default:
        //                {
        //                    logLevel = Microsoft.Extensions.Logging.LogLevel.Information;
        //                    break;
        //                }
        //        }

        //        _logger.Log(logLevel, messageProvider(), parameters);
        //        return true;
        //    };
        //}

        public IDisposable OpenNestedContext(string message)
        {
            return null;
        }

        public IDisposable OpenMappedContext(string key, string value)
        {
            return null;
        }

    }
}
