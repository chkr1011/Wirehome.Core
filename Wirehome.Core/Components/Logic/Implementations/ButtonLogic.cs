using System;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components.Adapters;
using Wirehome.Core.Constants;
using Wirehome.Core.Model;

namespace Wirehome.Core.Components.Logic.Implementations
{
    public class ButtonLogic : IComponentLogic
    {
        private readonly ComponentRegistryService _componentRegistryService;
        private readonly string _componentUid;
        private readonly IComponentAdapter _adapter;
        private readonly ILogger _logger;

        public ButtonLogic(string componentUid, IComponentAdapter adapter, ComponentRegistryService componentRegistryService, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            _componentUid = componentUid ?? throw new ArgumentNullException(nameof(componentUid));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _componentRegistryService = componentRegistryService;

            _logger = loggerFactory.CreateLogger<LampLogic>();

            adapter.MessageReceived += OnAdapterMessageReceived;
        }

        public WirehomeDictionary ExecuteCommand(WirehomeDictionary parameters)
        {
            var type = parameters["type"] as string;

            if (type == "initialize")
            {
                Initialize();
            }
            else
            {
                return new WirehomeDictionary().WithType(ControlType.NotSupportedException);
            }

            return new WirehomeDictionary().WithType(ControlType.Success);
        }

        private void Initialize()
        {
            _componentRegistryService.SetComponentStatus(_componentUid, ButtonModulePropertyUid.State, ButtonModulePropertyValue.Released);

            _adapter.SendMessage(new WirehomeDictionary().WithType(ControlType.Initialize));
        }

        private void OnAdapterMessageReceived(object sender, ComponentAdapterMessageReceivedEventArgs eventArgs)
        {
            var properties = eventArgs.Properties;

            var type = properties["type"] as string;
            if (type == "state_changed")
            {
                var state = properties["new_state"] as string; // TODO: Rename to "state".

                if (state == "pressed")
                {
                    _componentRegistryService.SetComponentStatus(_componentUid, ButtonModulePropertyUid.State, ButtonModulePropertyValue.Pressed);
                }
                else if (state == "released")
                {
                    _componentRegistryService.SetComponentStatus(_componentUid, ButtonModulePropertyUid.State, ButtonModulePropertyValue.Released);
                }
            }
        }
    }
}
