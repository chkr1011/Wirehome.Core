using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Scripting.Hosting;
using Wirehome.Core.Contracts;
using Wirehome.Core.Python.Proxies;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Python
{
    public class PythonEngineService : IService
    {
        private readonly ILogger _logger;

        private ScriptEngine _scriptEngine;

        public PythonEngineService(ILogger<PythonEngineService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Start()
        {
            _logger.LogInformation("Starting Python engine...");
            
            _scriptEngine = IronPython.Hosting.Python.CreateEngine();
            _scriptEngine.Runtime.IO.SetOutput(new PythonIOToLogStream(_logger), Encoding.UTF8);

            AddSearchPaths(_scriptEngine);

            var scriptHost = CreateScriptHost(new List<IPythonProxy>(), _logger);
            scriptHost.Compile("def test():\r\n    return 0");
            scriptHost.InvokeFunction("test");

            _logger.LogInformation("Python engine started.");
        }

        public PythonScriptHost CreateScriptHost(ICollection<IPythonProxy> pythonProxies, ILogger logger)
        {
            if (pythonProxies == null) throw new ArgumentNullException(nameof(pythonProxies));

            var scriptScope = _scriptEngine.CreateScope();

            pythonProxies = new List<IPythonProxy>(pythonProxies)
            {
                new LogPythonProxy(logger ?? _logger),
                new DebuggerPythonProxy()
            };

            var wirehomeWrapper = (IDictionary<string, object>)new ExpandoObject();

            foreach (var pythonProxy in pythonProxies)
            {
                wirehomeWrapper.Add(pythonProxy.ModuleName, pythonProxy);
            }

            scriptScope.SetVariable("wirehome", wirehomeWrapper);

            return new PythonScriptHost(scriptScope, wirehomeWrapper);
        }

        private static void AddSearchPaths(ScriptEngine scriptEngine)
        {
            // TODO: Consider adding custom paths via Settings file.

            var storagePaths = new StoragePaths();

            var librariesPath = Path.Combine(storagePaths.DataPath, "PythonLibraries");
            if (!Directory.Exists(librariesPath))
            {
                Directory.CreateDirectory(librariesPath);
            }

            var searchPaths = scriptEngine.GetSearchPaths();

            const string LinuxLibsPath = "/usr/lib/python2.7";
            if (Directory.Exists(LinuxLibsPath))
            {
                searchPaths.Add(LinuxLibsPath);
            }

            const string WindowsLibsPath = @"C:\Python27\Lib";
            if (Directory.Exists(WindowsLibsPath))
            {
                searchPaths.Add(WindowsLibsPath);
            }

            searchPaths.Add(librariesPath);
            scriptEngine.SetSearchPaths(searchPaths);
        }
    }
}
