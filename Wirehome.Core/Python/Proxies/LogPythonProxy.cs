#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using Microsoft.Extensions.Logging;

namespace Wirehome.Core.Python.Proxies
{
    public class LogPythonProxy : IPythonProxy
    {
        private readonly ILogger _logger;

        public LogPythonProxy(ILogger logger)
        {
            _logger = logger;
        }

        public string ModuleName { get; } = "log";
        
        public void debug(object message)
        {
            _logger.Log(LogLevel.Debug, Convert.ToString(message));
        }

        public void info(object message)
        {
            _logger.Log(LogLevel.Information, Convert.ToString(message));
        }

        public void information(object message)
        {
            _logger.Log(LogLevel.Information, Convert.ToString(message));
        }

        public void warning(object message)
        {
            _logger.Log(LogLevel.Warning, Convert.ToString(message));
        }

        public void error(object message)
        {
            _logger.Log(LogLevel.Error, Convert.ToString(message));
        }
    }
}