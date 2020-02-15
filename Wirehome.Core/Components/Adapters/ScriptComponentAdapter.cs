using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using System;
using Wirehome.Core.Python;

namespace Wirehome.Core.Components.Adapters
{
    public class ScriptComponentAdapter : IComponentAdapter
    {
        private readonly PythonScriptHostFactoryService _pythonScriptHostFactoryService;
        private readonly ComponentRegistryService _componentRegistryService;
        private readonly ILogger _logger;

        private PythonScriptHost _scriptHost;

        public ScriptComponentAdapter(PythonScriptHostFactoryService pythonScriptHostFactoryService, ComponentRegistryService componentRegistryService, ILogger<ScriptComponentAdapter> logger)
        {
            _pythonScriptHostFactoryService = pythonScriptHostFactoryService ?? throw new ArgumentNullException(nameof(pythonScriptHostFactoryService));
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Func<PythonDictionary, PythonDictionary> MessagePublishedCallback { get; set; }

        public void Compile(string componentUid, string script)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (script == null) throw new ArgumentNullException(nameof(script));

            _scriptHost = _pythonScriptHostFactoryService.CreateScriptHost(_logger, new ComponentPythonProxy(componentUid, _componentRegistryService));
            _scriptHost.SetVariable("publish_adapter_message", (PythonDelegates.CallbackWithResultDelegate)OnMessageReceived);
            _scriptHost.AddToWirehomeWrapper("publish_adapter_message", (PythonDelegates.CallbackWithResultDelegate)OnMessageReceived);

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

        public PythonDictionary ProcessMessage(PythonDictionary message)
        {
            return _scriptHost.InvokeFunction("process_adapter_message", message) as PythonDictionary ?? new PythonDictionary();
        }

        private PythonDictionary OnMessageReceived(PythonDictionary message)
        {
            return MessagePublishedCallback?.Invoke(message) as PythonDictionary ?? new PythonDictionary();
        }
    }
}
