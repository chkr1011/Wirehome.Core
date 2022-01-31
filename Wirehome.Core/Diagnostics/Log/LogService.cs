using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Wirehome.Core.Contracts;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Diagnostics.Log
{
    public sealed class LogService : WirehomeCoreService
    {
        readonly LinkedList<LogEntry> _logEntries = new();
        readonly SystemStatusService _systemStatusService;
        readonly LogServiceOptions _options;

        int _informationCount;
        int _warningsCount;
        int _errorsCount;

        public LogService(StorageService storageService, SystemStatusService systemStatusService)
        {
            _systemStatusService = systemStatusService ?? throw new ArgumentNullException(nameof(systemStatusService));

            if (storageService is null) throw new ArgumentNullException(nameof(storageService));
            if (!storageService.SafeReadSerializedValue(out _options, DefaultDirectoryNames.Configuration, LogServiceOptions.Filename))
            {
                _options = new LogServiceOptions();
            }
        }

        public void Publish(DateTime timestamp, LogLevel logLevel, string source, string message, Exception exception)
        {
            var newLogEntry = new LogEntry(timestamp, logLevel, source, message, exception?.ToString());

            lock (_logEntries)
            {
                if (newLogEntry.Level == LogLevel.Error)
                {
                    _errorsCount++;
                }
                else if (newLogEntry.Level == LogLevel.Warning)
                {
                    _warningsCount++;
                }
                else if (newLogEntry.Level == LogLevel.Information)
                {
                    _informationCount++;
                }

                _logEntries.AddFirst(newLogEntry);

                if (_logEntries.Count > _options.MessageCount)
                {
                    var removedLogEntry = _logEntries.Last.Value;

                    if (removedLogEntry.Level == LogLevel.Error)
                    {
                        _errorsCount--;
                    }
                    else if (removedLogEntry.Level == LogLevel.Warning)
                    {
                        _warningsCount--;
                    }
                    else if (removedLogEntry.Level == LogLevel.Information)
                    {
                        _informationCount--;
                    }

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

                _informationCount = 0;
                _warningsCount = 0;
                _errorsCount = 0;

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

                    if (logEntries.Count >= filter.TakeCount)
                    {
                        break;
                    }
                }
            }

            return logEntries;
        }

        void UpdateSystemStatus()
        {
            _systemStatusService.Set("log.information_count", _informationCount);
            _systemStatusService.Set("log.warnings_count", _warningsCount);
            _systemStatusService.Set("log.errors_count", _errorsCount);
        }
    }
}
