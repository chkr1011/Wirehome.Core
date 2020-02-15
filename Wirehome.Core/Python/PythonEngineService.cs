using Microsoft.Extensions.Logging;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text;
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

            // The light weight scopes store the variables in a field or an array instead of storing
            // them in static fields.To do that we need to pass in the field or close over it and then
            // on each access we need to do the field or array access(and if it's an array access, it
            // needs to be bounds checked).  But the end result is that the only thing which is keeping
            // the global variable / call site objects alive are the delegate which implements the
            // ScriptCode.Once the ScriptCode goes away all of those call sites and PythonGlobal
            // objects can be collected.
            // 
            // So lightweight scopes come at a performance cost, but they are more applicable
            // Where you're actually re-compiling code regularly.
            _scriptEngine = IronPython.Hosting.Python.CreateEngine(new Dictionary<string, object>
            {
                ["LightweightScopes"] = true
            });

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

        private void AddSearchPaths(ScriptEngine scriptEngine)
        {
            var storagePaths = new StoragePaths();

            var paths = new List<string>
            {
                Path.Combine(storagePaths.DataPath, "PythonLibraries")
            };

            AddSearchPaths(scriptEngine, paths);
        }

        private void AddSearchPaths(ScriptEngine scriptEngine, IEnumerable<string> paths)
        {
            var searchPaths = scriptEngine.GetSearchPaths();

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    searchPaths.Add(path);
                    _logger.LogInformation($"Added Python lib path: {path}");
                }
            }

            scriptEngine.SetSearchPaths(searchPaths);
        }
    }
}
