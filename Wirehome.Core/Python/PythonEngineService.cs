using Microsoft.Extensions.Logging;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Python.Proxies;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Python
{
    public sealed class PythonEngineService : WirehomeCoreService
    {
        readonly ILogger _logger;
        readonly PythonIOToLogStream _pythonIOToLogStream;
        readonly LogPythonProxy _logPythonProxy;

        ScriptEngine _scriptEngine;

        int _createdScriptHostsCount;

        public PythonEngineService(SystemStatusService systemStatusService, ILogger<PythonEngineService> logger)
        {
            if (systemStatusService == null) throw new ArgumentNullException(nameof(systemStatusService));
            systemStatusService.Set("python_engine.created_script_hosts_count", () => _createdScriptHostsCount);

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logPythonProxy = new LogPythonProxy(_logger);
            _pythonIOToLogStream = new PythonIOToLogStream(_logger);
        }

        public PythonScriptHost CreateScriptHost(ICollection<IPythonProxy> pythonProxies)
        {
            if (pythonProxies == null) throw new ArgumentNullException(nameof(pythonProxies));

            var allPythonProxies = new List<IPythonProxy>(pythonProxies)
            {
                _logPythonProxy,
                new DebuggerPythonProxy()
            };

            _createdScriptHostsCount++;
            return new PythonScriptHost(_scriptEngine, allPythonProxies);
        }

        protected override void OnStart()
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
                ["LightweightScopes"] = false
            });

            _scriptEngine.Runtime.IO.SetOutput(_pythonIOToLogStream, Encoding.UTF8);

            AddSearchPaths(_scriptEngine);

            RunTestScript();

            _logger.LogInformation("Python engine started.");
        }

        void RunTestScript()
        {
            var testScript = new StringBuilder();
            testScript.AppendLine("def test():");
            testScript.AppendLine("    wirehome.log.information('Test message from test script.')");
            testScript.AppendLine("    return 1234");

            var scriptHost = CreateScriptHost(new List<IPythonProxy>());
            scriptHost.Compile(testScript.ToString());
            var result = scriptHost.InvokeFunction("test");

            if (result as int? != 1234)
            {
                throw new InvalidOperationException("Python test script failed.");
            }
        }
        
        void AddSearchPaths(ScriptEngine scriptEngine)
        {
            var storagePaths = new StoragePaths();

            var paths = new List<string>
            {
                Path.Combine(storagePaths.DataPath, "PythonLibraries")
            };

            AddSearchPaths(scriptEngine, paths);
        }

        void AddSearchPaths(ScriptEngine scriptEngine, IEnumerable<string> paths)
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
