using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using System;
using Wirehome.Core.Constants;
using Wirehome.Core.Foundation.Model;
using Wirehome.Core.Python;

namespace Wirehome.Core.Components.Logic
{
    public class ScriptComponentLogic : IComponentLogic
    {
        private const string ProcessLogicMessageFunctionName = "process_logic_message";
        private const string ProcessAdapterMessageFunctionName = "process_adapter_message";
        private const string GetDebugInformationFunctionName = "get_debug_information";

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

        public PythonDelegates.CallbackWithResultDelegate AdapterMessagePublishedCallback { get; set; }

        public void Compile(string componentUid, string script)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (script == null) throw new ArgumentNullException(nameof(script));

            _scriptHost = _pythonScriptHostFactoryService.CreateScriptHost(_logger, new ComponentPythonProxy(componentUid, _componentRegistryService));
            _scriptHost.SetVariable("publish_adapter_message", (PythonDelegates.CallbackWithResultDelegate)OnAdapterMessagePublished);
            _scriptHost.AddToWirehomeWrapper("publish_adapter_message", (PythonDelegates.CallbackWithResultDelegate)OnAdapterMessagePublished);

            // TODO: Consider adding debugger here and move enable/disable/getTrace to IComponentLogic and enable via HTTP API.

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
            var result = _scriptHost.InvokeFunction(ProcessLogicMessageFunctionName, message);
            return result as WirehomeDictionary ?? new WirehomeDictionary();
        }

        public WirehomeDictionary GetDebugInformation(WirehomeDictionary parameters)
        {
            if (!_scriptHost.FunctionExists(GetDebugInformationFunctionName))
            {
                return new WirehomeDictionary().WithType(ControlType.NotSupportedException);
            }

            if (!(_scriptHost.InvokeFunction(GetDebugInformationFunctionName, parameters) is WirehomeDictionary result))
            {
                return new WirehomeDictionary().WithType(ControlType.ReturnValueTypeMismatchException);
            }

            return result;
        }

        public WirehomeDictionary ProcessAdapterMessage(WirehomeDictionary message)
        {
            var result = _scriptHost.InvokeFunction(ProcessAdapterMessageFunctionName, message);
            return result as WirehomeDictionary ?? new WirehomeDictionary();
        }

        private PythonDictionary OnAdapterMessagePublished(PythonDictionary message)
        {
            var result = AdapterMessagePublishedCallback?.Invoke(message);
            return result ?? new PythonDictionary();
        }
    }
}
