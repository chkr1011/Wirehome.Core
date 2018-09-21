using System;
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

        public event EventHandler<ComponentAdapterMessageReceivedEventArgs> MessageReceived;

        public void Initialize(Component component, string script)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            if (script == null) throw new ArgumentNullException(nameof(script));

            _scriptHost = _pythonEngineService.CreateScriptHost(_logger, new ComponentPythonProxy(component.Uid, _componentRegistryService));

            var proxy = new LogicPythonProxy(OnMessageReceived);
            _scriptHost.SetVariable(proxy.ModuleName, proxy);
            _scriptHost.SetVariable("publish_adapter_message", (Func<WirehomeDictionary, WirehomeDictionary>)OnMessageReceived);

            _scriptHost.Initialize(script);
        }

        public void SetVariable(string name, object value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            _scriptHost.SetVariable(name, value);
        }

        public WirehomeDictionary SendMessage(WirehomeDictionary properties)
        {
            var result = _scriptHost.InvokeFunction("process_adapter_message", properties);
            return result as WirehomeDictionary ?? new WirehomeDictionary();
        }

        private WirehomeDictionary OnMessageReceived(WirehomeDictionary properties)
        {
            var eventArgs = new ComponentAdapterMessageReceivedEventArgs(properties);
            MessageReceived?.Invoke(this, eventArgs);
            return eventArgs.Result;
        }
    }
}
