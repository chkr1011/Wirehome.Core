using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using System;
using Wirehome.Core.Components.Configuration;
using Wirehome.Core.Constants;
using Wirehome.Core.Python;

namespace Wirehome.Core.Components.Logic
{
    public sealed class ScriptComponentLogic : IComponentLogic
    {
        const string ProcessLogicMessageFunctionName = "process_logic_message";
        const string ProcessAdapterMessageFunctionName = "process_adapter_message";
        const string GetDebugInformationFunctionName = "get_debug_information";

        readonly ComponentLogicConfiguration _logicConfiguration;
        readonly PythonScriptHostFactoryService _pythonScriptHostFactoryService;
        readonly ComponentRegistryService _componentRegistryService;
        readonly ILogger _logger;

        PythonScriptHost _scriptHost;

        public ScriptComponentLogic(
            ComponentLogicConfiguration logicConfiguration,
            PythonScriptHostFactoryService pythonScriptHostFactoryService,
            ComponentRegistryService componentRegistryService,
            ILogger<ScriptComponentLogic> logger)
        {
            _logicConfiguration = logicConfiguration ?? throw new ArgumentNullException(nameof(logicConfiguration));
            _pythonScriptHostFactoryService = pythonScriptHostFactoryService ?? throw new ArgumentNullException(nameof(pythonScriptHostFactoryService));
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Id => _logicConfiguration.Uid.Id;

        public PythonDelegates.CallbackWithResultDelegate AdapterMessagePublishedCallback { get; set; }

        public void Compile(string componentUid, string script)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (script == null) throw new ArgumentNullException(nameof(script));

            _scriptHost = _pythonScriptHostFactoryService.CreateScriptHost(new ComponentPythonProxy(componentUid, _componentRegistryService));
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

        public PythonDictionary ProcessMessage(PythonDictionary message)
        {
            return _scriptHost.InvokeFunction(ProcessLogicMessageFunctionName, message) as PythonDictionary ?? new PythonDictionary();
        }

        public PythonDictionary ProcessAdapterMessage(PythonDictionary message)
        {
            return _scriptHost.InvokeFunction(ProcessAdapterMessageFunctionName, message) as PythonDictionary ?? new PythonDictionary();
        }

        public PythonDictionary GetDebugInformation(PythonDictionary parameters)
        {
            if (!_scriptHost.FunctionExists(GetDebugInformationFunctionName))
            {
                return new PythonDictionary
                {
                    ["type"] = WirehomeMessageType.NotSupportedException
                };
            }

            if (!(_scriptHost.InvokeFunction(GetDebugInformationFunctionName, parameters) is PythonDictionary result))
            {
                return new PythonDictionary
                {
                    ["type"] = WirehomeMessageType.ReturnValueTypeMismatchException
                };
            }

            return result;
        }

        PythonDictionary OnAdapterMessagePublished(PythonDictionary message)
        {
            return AdapterMessagePublishedCallback?.Invoke(message) ?? new PythonDictionary();
        }
    }
}
