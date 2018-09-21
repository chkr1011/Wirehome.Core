using System;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components.Adapters;
using Wirehome.Core.Constants;
using Wirehome.Core.Model;

namespace Wirehome.Core.Components.Logic.Implementations
{
    public class SocketLogic : IComponentLogic
    {
        private readonly ComponentRegistryService _componentRegistryService;
        private readonly string _componentUid;
        private readonly IComponentAdapter _adapter;

        private readonly ILogger _logger;

        public SocketLogic(string componentUid, IComponentAdapter adapter, ComponentRegistryService componentRegistryService, ILoggerFactory loggerFactory)
        {
            _componentUid = componentUid ?? throw new ArgumentNullException(nameof(componentUid));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<LampLogic>();

            adapter.MessageReceived += Adapter_MessageReceived;
        }

        private void Adapter_MessageReceived(object sender, ComponentAdapterMessageReceivedEventArgs e)
        {
            if (e.Properties.IsOfType("turned_on"))
            {
                _componentRegistryService.SetComponentStatus(_componentUid, "power.state", "on");
            }
            else if (e.Properties.IsOfType("turned_off"))
            {
                _componentRegistryService.SetComponentStatus(_componentUid, "power.state", "off");
                _componentRegistryService.SetComponentStatus(_componentUid, "power.consumption", 0);
            }
        }

        public WirehomeDictionary ExecuteCommand(WirehomeDictionary parameters)
        {
            var type = parameters["type"] as string;

            if (type == "initialize")
            {
                return Initialize();
            }

            if (type == "turn_on")
            {
                return TurnOn();
            }

            if (type == "turn_off")
            {
                return TurnOff();
            }

            if (type == "toggle")
            {
                return Toggle();
            }

            return new WirehomeDictionary().WithType(ControlType.NotSupportedException);
        }

        private WirehomeDictionary Initialize()
        {
            _componentRegistryService.SetComponentStatus(_componentUid, "power.state", "unknown");
            _componentRegistryService.SetComponentStatus(_componentUid, "power.consumption", "unknown");

            var adapterResult = _adapter.SendMessage(new WirehomeDictionary().WithType(ControlType.Initialize));

            if (adapterResult.IsSuccess())
            {
                _componentRegistryService.SetComponentStatus(_componentUid, "power.state", "off");
                _componentRegistryService.SetComponentStatus(_componentUid, "power.consumption", null);
            }
            else
            {
                return adapterResult;
            }

            return new WirehomeDictionary().WithType(ControlType.Success);
        }

        private WirehomeDictionary TurnOn()
        {
            var adapterResult = _adapter.SendMessage(new WirehomeDictionary().WithType("turn_on"));

            if (adapterResult.IsSuccess())
            {
                _componentRegistryService.SetComponentStatus(_componentUid, "power.state", "on");
            }
            else
            {
                return adapterResult;
            }

            return new WirehomeDictionary().WithType(ControlType.Success);
        }

        private WirehomeDictionary TurnOff()
        {
            var adapterResult = _adapter.SendMessage(new WirehomeDictionary().WithType("turn_off"));

            if (adapterResult.IsSuccess())
            {
                _componentRegistryService.SetComponentStatus(_componentUid, "power.state", "off");
                _componentRegistryService.SetComponentStatus(_componentUid, "power.consumption", 0);
            }
            else
            {
                return adapterResult;
            }

            return new WirehomeDictionary().WithType(ControlType.Success);
        }

        private WirehomeDictionary Toggle()
        {
            var powerState = _componentRegistryService.GetComponentStatus(_componentUid, "power.state");
            if (powerState as string == "off")
            {
                return TurnOn();
            }

            return TurnOff();
        }
    }
}
