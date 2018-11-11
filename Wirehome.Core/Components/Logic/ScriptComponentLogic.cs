using System;
using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components.Adapters;
using Wirehome.Core.Model;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Proxies;

namespace Wirehome.Core.Components.Logic
{
    public class ScriptComponentLogic : IComponentLogic
    {
        private readonly PythonEngineService _pythonEngineService;
        private readonly ComponentRegistryService _componentRegistryService;
        private readonly ILogger _logger;

        private PythonScriptHost _scriptHost;

        public ScriptComponentLogic(PythonEngineService pythonEngineService, ComponentRegistryService componentRegistryService, ILoggerFactory loggerFactory)
        {
            _pythonEngineService = pythonEngineService ?? throw new ArgumentNullException(nameof(pythonEngineService));
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<ScriptComponentAdapter>();
        }

        public Func<WirehomeDictionary, WirehomeDictionary> AdapterMessagePublishedCallback { get; set; }

        public void Initialize(string componentUid, string script)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (script == null) throw new ArgumentNullException(nameof(script));

            _scriptHost = _pythonEngineService.CreateScriptHost(_logger, new ComponentPythonProxy(componentUid, _componentRegistryService));
            _scriptHost.SetVariable("publish_adapter_message", (PythonScriptHost.CallbackWithResultDelegate)OnAdapterMessagePublished);
            _scriptHost.WirehomeWrapper.Add("publish_adapter_message", (PythonScriptHost.CallbackWithResultDelegate)OnAdapterMessagePublished);

            _scriptHost.Initialize(script);
        }

        public void SetVariable(string name, object value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            _scriptHost.SetVariable(name, value);
        }

        public WirehomeDictionary ProcessMessage(WirehomeDictionary message)
        {
            var result = _scriptHost.InvokeFunction("process_logic_message", message);
            return result as WirehomeDictionary ?? new WirehomeDictionary();
        }

        public WirehomeDictionary ProcessAdapterMessage(WirehomeDictionary message)
        {
            var result = _scriptHost.InvokeFunction("process_adapter_message", message);
            return result as WirehomeDictionary ?? new WirehomeDictionary();
        }

        private PythonDictionary OnAdapterMessagePublished(PythonDictionary message)
        {
            var result = AdapterMessagePublishedCallback?.Invoke(message);
            return result ?? new PythonDictionary();
        }
    }
}
