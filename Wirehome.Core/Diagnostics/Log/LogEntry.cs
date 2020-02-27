using Microsoft.Extensions.Logging;
using System;

namespace Wirehome.Core.Diagnostics.Log
{
    public class LogEntry
    {
        public LogEntry(DateTime timestamp, LogLevel level, string source, string message, string exception)
        {
            Timestamp = timestamp;
            Level = level;
            Source = source;
            Message = message;
            Exception = exception;
        }

        public DateTime Timestamp { get; }

        public LogLevel Level { get; }

        public string Source { get; }

        public string Message { get; }

        public string Exception { get; }
    }
}