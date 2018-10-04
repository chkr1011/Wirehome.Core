using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Scripting.Hosting;
using Wirehome.Core.Python.Proxies;

namespace Wirehome.Core.Python
{
    public class PythonEngineService
    {
        private readonly List<Func<IPythonProxy>> _proxyCreators = new List<Func<IPythonProxy>>();
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        private ScriptEngine _scriptEngine;

        public PythonEngineService(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

            _logger = loggerFactory.CreateLogger<PythonEngineService>();
        }

        public void Start()
        {
            _logger.Log(LogLevel.Information, "Starting Python engine...");

            _scriptEngine = IronPython.Hosting.Python.CreateEngine();
            _scriptEngine.Runtime.IO.SetOutput(new PythonIOToLogStream(_logger), Encoding.UTF8);

            var scriptHost = CreateScriptHost(_logger);
            scriptHost.Initialize("def test():\r\n    return 0");
            scriptHost.InvokeFunction("test");

            _logger.Log(LogLevel.Information, "Python engine started.");
        }

        public void RegisterTransientProxy(Func<IPythonProxy> proxyCreator)
        {
            if (proxyCreator == null) throw new ArgumentNullException(nameof(proxyCreator));

            _proxyCreators.Add(proxyCreator);
        }

        public void RegisterSingletonProxy(IPythonProxy proxy)
        {
            if (proxy == null) throw new ArgumentNullException(nameof(proxy));

            _proxyCreators.Add(() => proxy);
        }

        public PythonScriptHost CreateScriptHost(params IPythonProxy[] customProxies)
        {
            return CreateScriptHost(null, customProxies);
        }

        public PythonScriptHost CreateScriptHost(ILogger logger, params IPythonProxy[] customProxies)
        {
            var scriptScope = _scriptEngine.CreateScope();

            // Create a trace python proxy which collects traces for the current instance only.
            var defaultProxies = new List<IPythonProxy>
            {
                new ConverterPythonProxy(),
                new DateTimePythonProxy(),
                new DateTimeParserPythonProxy(),
                new DataProviderPythonProxy(),
                new LogPythonProxy(logger ?? _logger)
            };

            foreach (var proxy in defaultProxies)
            {
                scriptScope.SetVariable(proxy.ModuleName, proxy);
            }

            foreach (var proxyCreator in _proxyCreators)
            {
                var proxy = proxyCreator();
                scriptScope.SetVariable(proxy.ModuleName, proxy);
            }

            foreach (var proxy in customProxies)
            {
                scriptScope.SetVariable(proxy.ModuleName, proxy);
            }

            return new PythonScriptHost(scriptScope, _loggerFactory);
        }
    }
}
