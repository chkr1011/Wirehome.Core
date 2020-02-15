#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using Microsoft.Extensions.Logging;
using System;
using System.Globalization;

namespace Wirehome.Core.Python.Proxies
{
    public class LogPythonProxy : IPythonProxy
    {
        readonly ILogger _logger;

        public LogPythonProxy(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string ModuleName { get; } = "log";

        public void trace(object message)
        {
            _logger.Log(LogLevel.Trace, Convert.ToString(message, CultureInfo.InvariantCulture));
        }

        public void debug(object message)
        {
            _logger.Log(LogLevel.Debug, Convert.ToString(message, CultureInfo.InvariantCulture));
        }

        public void info(object message)
        {
            _logger.Log(LogLevel.Information, Convert.ToString(message, CultureInfo.InvariantCulture));
        }

        public void information(object message)
        {
            _logger.Log(LogLevel.Information, Convert.ToString(message, CultureInfo.InvariantCulture));
        }

        public void warning(object message)
        {
            _logger.Log(LogLevel.Warning, Convert.ToString(message, CultureInfo.InvariantCulture));
        }

        public void error(object message)
        {
            _logger.Log(LogLevel.Error, Convert.ToString(message, CultureInfo.InvariantCulture));
        }
    }
}