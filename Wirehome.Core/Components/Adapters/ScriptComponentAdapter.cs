using System;
using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Model;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Proxies;

namespace Wirehome.Core.Components.Adapters
{
    public class ScriptComponentAdapter : IComponentAdapter
    {
        private readonly PythonEngineService _pythonEngineService;
        private readonly ComponentRegistryService _componentRegistryService;
        private readonly ILogger _logger;

        private PythonScriptHost _scriptHost;

        public ScriptComponentAdapter(PythonEngineService pythonEngineService, ComponentRegistryService componentRegistryService, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _pythonEngineService = pythonEngineService ?? throw new ArgumentNullException(nameof(pythonEngineService));
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));

            _logger = loggerFactory.CreateLogger<ScriptComponentAdapter>();
        }

        public Func<WirehomeDictionary, WirehomeDictionary> MessagePublishedCallback { get; set; }

        public void Initialize(string componentUid, string script)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (script == null) throw new ArgumentNullException(nameof(script));

            _scriptHost = _pythonEngineService.CreateScriptHost(_logger, new ComponentPythonProxy(componentUid, _componentRegistryService));
            _scriptHost.SetVariable("publish_adapter_message", (Func<PythonDictionary, PythonDictionary>)OnMessageReceived);

            _scriptHost.Initialize(script);
        }

        public void SetVariable(string name, object value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            _scriptHost.SetVariable(name, value);
        }

        public WirehomeDictionary ProcessMessage(WirehomeDictionary message)
        {
            var result = _scriptHost.InvokeFunction("process_adapter_message", message);
            return result as WirehomeDictionary ?? new WirehomeDictionary();
        }

        private PythonDictionary OnMessageReceived(PythonDictionary message)
        {
            var result = MessagePublishedCallback?.Invoke(message);
            return result ?? new PythonDictionary();
        }
    }
}
