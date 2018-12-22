using System;
using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Model;
using Wirehome.Core.Python;

namespace Wirehome.Core.Components.Logic
{
    public class ScriptComponentLogic : IComponentLogic
    {
        private readonly PythonScriptHostFactoryService _pythonScriptHostFactoryService;
        private readonly ComponentRegistryService _componentRegistryService;
        private readonly ILogger _logger;

        private PythonScriptHost _scriptHost;

        public ScriptComponentLogic(
            PythonScriptHostFactoryService pythonScriptHostFactoryService,
            ComponentRegistryService componentRegistryService, 
            ILogger<ScriptComponentLogic> logger)
        {
            _pythonScriptHostFactoryService = pythonScriptHostFactoryService ?? throw new ArgumentNullException(nameof(pythonScriptHostFactoryService));
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));            
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Func<WirehomeDictionary, WirehomeDictionary> AdapterMessagePublishedCallback { get; set; }

        public void Compile(string componentUid, string script)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (script == null) throw new ArgumentNullException(nameof(script));

            _scriptHost = _pythonScriptHostFactoryService.CreateScriptHost(_logger, new ComponentPythonProxy(componentUid, _componentRegistryService));
            _scriptHost.SetVariable("publish_adapter_message", (PythonDelegates.CallbackWithResultDelegate)OnAdapterMessagePublished);
            _scriptHost.AddToWirehomeWrapper("publish_adapter_message", (PythonDelegates.CallbackWithResultDelegate)OnAdapterMessagePublished);

            _scriptHost.Compile(script);
        }

        public void SetVariable(string name, object value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            _scriptHost.SetVariable(name, value);
        }

        public void AddToWirehomeWrapper(string name, object value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            _scriptHost.AddToWirehomeWrapper(name, value);
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
