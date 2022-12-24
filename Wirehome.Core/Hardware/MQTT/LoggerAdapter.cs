using System;
using Microsoft.Extensions.Logging;
using MQTTnet.Diagnostics;

namespace Wirehome.Core.Hardware.MQTT;

public sealed class LoggerAdapter : IMqttNetLogger
{
    readonly ILogger _logger;

    public LoggerAdapter(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool IsEnabled { get; } = true;

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

        _logger.Log(newLogLevel, source, exception, message, parameters);
    }
}