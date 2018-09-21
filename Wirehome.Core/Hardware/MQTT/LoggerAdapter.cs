using System;
using Microsoft.Extensions.Logging;
using MQTTnet.Diagnostics;

namespace Wirehome.Core.Hardware.MQTT
{
    public class LoggerAdapter : IMqttNetLogger
    {
        private readonly ILogger _logger;

        public LoggerAdapter(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public IMqttNetChildLogger CreateChildLogger(string source = null)
        {
            return new MqttNetChildLogger(this, source);
        }

        public void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception)
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

            //_logger.Log(newLogLevel, exception, message, parameters);
        }

        public event EventHandler<MqttNetLogMessagePublishedEventArgs> LogMessagePublished;
    }
}
