using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Contracts;
using Wirehome.Core.Hardware.MQTT;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Diagnostics.Log;

public sealed class LogService : WirehomeCoreService
{
    readonly LinkedList<LogEntry> _logEntries = new();
    
    readonly LogServiceOptions _options;
    readonly SystemStatusService _systemStatusService;
    
    int _errorsCount;
    int _informationCount;
    int _warningsCount;

    public LogService(StorageService storageService, SystemStatusService systemStatusService, MqttService mqttService)
    {
        _systemStatusService = systemStatusService ?? throw new ArgumentNullException(nameof(systemStatusService));

        Sender = new LogSender(mqttService);
        
        if (storageService is null)
        {
            throw new ArgumentNullException(nameof(storageService));
        }
        
        if (!storageService.SafeReadSerializedValue(out _options, DefaultDirectoryNames.Configuration, LogServiceOptions.Filename))
        {
            _options = new LogServiceOptions();
        }
    }
    
    public LogSender Sender { get; }
    
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
        if (filter == null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        var logEntries = new List<LogEntry>();

        lock (_logEntries)
        {
            foreach (var logEntry in _logEntries)
            {
                if (logEntry.Level == LogLevel.Information && !filter.IncludeInformation)
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

    public void Publish(DateTime timestamp, LogLevel logLevel, string source, string message, Exception exception)
    {
        var newLogEntry = new LogEntry
        {
            Timestamp = timestamp,
            Level = logLevel,
            Source = source,
            Message = message,
            Exception = exception?.ToString()
        };
        
        Sender.TrySend(newLogEntry);
        
        // Debug level is not tracked in history.
        if (logLevel < LogLevel.Information)
        {
            return;
        }

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
    
    void UpdateSystemStatus()
    {
        _systemStatusService.Set("log.information_count", _informationCount);
        _systemStatusService.Set("log.warnings_count", _warningsCount);
        _systemStatusService.Set("log.errors_count", _errorsCount);
    }
}