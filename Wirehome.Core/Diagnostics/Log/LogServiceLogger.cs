using System;
using Microsoft.Extensions.Logging;

namespace Wirehome.Core.Diagnostics.Log;

public class LogServiceLogger : ILogger
{
    readonly string _categoryName;
    readonly LogService _logService;

    public LogServiceLogger(LogService logService, string categoryName)
    {
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));

        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel.Debug;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (formatter == null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }

        if (!IsEnabled(logLevel))
        {
            return;
        }

        _logService.Publish(DateTime.UtcNow, logLevel, _categoryName, formatter(state, exception), exception);
    }
}