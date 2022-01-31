using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Wirehome.Core.Diagnostics.Log
{
    public sealed class LogServiceLoggerProvider : ILoggerProvider
    {
        readonly Dictionary<string, ILogger> _loggers = new();

        readonly LogService _logService;

        public LogServiceLoggerProvider(LogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (categoryName == null)
            {
                categoryName = string.Empty;
            }

            lock (_loggers)
            {
                if (_loggers.TryGetValue(categoryName, out var logger))
                {
                    return logger;
                }

                logger = new LogServiceLogger(_logService, categoryName);
                _loggers.Add(categoryName, logger);

                return logger;
            }
        }

        public void Dispose()
        {
        }
    }
}
