using Microsoft.Extensions.Logging;
using MQTTnet.Diagnostics;
using System;
using System.Collections.Generic;

namespace Wirehome.Core.Hardware.MQTT
{
    public class LoggerAdapter : IMqttNetLogger
    {
        private readonly Dictionary<string, IMqttNetLogger> _childLoggers = new Dictionary<string, IMqttNetLogger>();
        private readonly ILogger _logger;

        public LoggerAdapter(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public event EventHandler<MqttNetLogMessagePublishedEventArgs> LogMessagePublished;

        public IMqttNetLogger CreateChildLogger(string source = null)
        {
            if (source == null)
            {
                // Required to avoid argument null exception in dictionary.
                source = string.Empty;
            }

            lock (_childLoggers)
            {
                if (!_childLoggers.TryGetValue(source, out var childLogger))
                {
                    childLogger = new MqttNetLogger(source);
                    _childLoggers[source] = childLogger;
                }

                return childLogger;
            }
        }

        public void Publish(MqttNetLogLevel logLevel, string message, object[] parameters, Exception exception)
        {
            var newLogLevel = LogLevel.Debug;

            if (logLevel == MqttNetLogLevel.Warning)
            {
                newLogLevel = LogLevel.Warning;
            }
            else if (logLevel == MqttNetLogLevel.Error)
            {
                newLogLevel = LogLevel.Error;
            }
            else if (logLevel == MqttNetLogLevel.Info)
            {
                newLogLevel = LogLevel.Information;
            }
            else if (logLevel == MqttNetLogLevel.Verbose)
            {
                newLogLevel = LogLevel.Trace;
            }

            _logger.Log(newLogLevel, exception, message, parameters);
        }

        public void NotifyLogMessagePublished(MqttNetLogMessage logMessage)
        {
            LogMessagePublished?.Invoke(this, new MqttNetLogMessagePublishedEventArgs(logMessage));
        }
    }
}
