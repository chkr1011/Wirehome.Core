using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Wirehome.Core.Diagnostics.Log
{
    public class LogService
    {
        private readonly LinkedList<LogEntry> _logEntries = new LinkedList<LogEntry>();

        private readonly SystemStatusService _systemStatusService;

        public LogService(SystemStatusService systemStatusService)
        {
            _systemStatusService = systemStatusService ?? throw new ArgumentNullException(nameof(systemStatusService));
        }

        public void Publish(DateTime timestamp, LogLevel logLevel, string source, string message, Exception exception)
        {
            lock (_logEntries)
            {
                _logEntries.AddFirst(new LogEntry(timestamp, logLevel, source, message, exception?.ToString()));

                if (_logEntries.Count > 1000)
                {
                    _logEntries.RemoveLast();
                }

                UpdateSystemStatus();
            }
        }

        public void Clear()
        {
            lock (_logEntries)
            {
                _logEntries.Clear();
                UpdateSystemStatus();
            }
        }

        public List<LogEntry> GetEntries(LogEntryFilter filter)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            var logEntries = new List<LogEntry>();

            lock (_logEntries)
            {
                foreach (var logEntry in _logEntries)
                {
                    if (logEntry.Level == LogLevel.Information && !filter.IncludeInformations)
                    {
                        continue;
                    }

                    if (logEntry.Level == LogLevel.Warning && !filter.IncludeWarnings)
                    {
                        continue;
                    }

                    if (logEntry.Level == LogLevel.Error && !filter.IncludeErrors)
                    {
                        continue;
                    }

                    logEntries.Add(logEntry);
                }
            }

            return logEntries;
        }

        private void UpdateSystemStatus()
        {
            var informationsCount = 0;
            var warningsCount = 0;
            var errorsCount = 0;

            foreach (var logEntry in _logEntries)
            {
                if (logEntry.Level == LogLevel.Error)
                {
                    errorsCount++;
                }
                else if (logEntry.Level == LogLevel.Warning)
                {
                    warningsCount++;
                }
                else if (logEntry.Level == LogLevel.Information)
                {
                    informationsCount++;
                }
            }

            _systemStatusService.Set("log.informations_count", informationsCount);
            _systemStatusService.Set("log.warnings_count", warningsCount);
            _systemStatusService.Set("log.errors_count", errorsCount);
        }
    }
}
