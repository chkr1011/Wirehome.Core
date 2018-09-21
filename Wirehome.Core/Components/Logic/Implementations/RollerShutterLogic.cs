using System;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components.Adapters;
using Wirehome.Core.Constants;
using Wirehome.Core.Model;

namespace Wirehome.Core.Components.Logic.Implementations
{
    public class RollerShutterLogic : IComponentLogic
    {
        private readonly ComponentRegistryService _componentRegistryService;
        private readonly string _componentUid;
        private readonly IComponentAdapter _adapter;

        private readonly ILogger _logger;

        public RollerShutterLogic(string componentUid, IComponentAdapter adapter, ComponentRegistryService componentRegistryService, ILoggerFactory loggerFactory)
        {
            _componentUid = componentUid ?? throw new ArgumentNullException(nameof(componentUid));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<RollerShutterLogic>();
        }

        public WirehomeDictionary ExecuteCommand(WirehomeDictionary parameters)
        {
            if (parameters.IsOfType("initialize"))
            {
                Initialize();
            }
            else if (parameters.IsOfType("turn_off"))
            {
                return TurnOff();
            }
            else if (parameters.IsOfType("move_up"))
            {
                return MoveUp();
            }
            else if (parameters.IsOfType("move_down"))
            {
                return MoveDown();
            }
            else
            {
                return new WirehomeDictionary().WithType(ControlType.NotSupportedException);
            }

            return new WirehomeDictionary().WithType(ControlType.Success);
        }

        private void Initialize()
        {
            _componentRegistryService.SetComponentStatus(_componentUid, "roller_shutter.state", "unknown");
            _componentRegistryService.SetComponentStatus(_componentUid, "roller_shutter.position", "unknown");
            _componentRegistryService.SetComponentStatus(_componentUid, "power.state", "unknown");
            _componentRegistryService.SetComponentStatus(_componentUid, "power.consumption", "unknown");

            if (_adapter.SendMessage(new WirehomeDictionary().WithType(ControlType.Initialize)).IsSuccess())
            {
                TurnOff();
            }
        }

        private WirehomeDictionary TurnOff()
        {
            var adapterResult = _adapter.SendMessage(new WirehomeDictionary().WithType("turn_off"));
            if (adapterResult.IsSuccess())
            {
                _componentRegistryService.SetComponentStatus(_componentUid, "roller_shutter.state", "off");
                //_componentRegistryService.SetComponentProperty(_componentUid, "roller_shutter.position", "unknown");
                _componentRegistryService.SetComponentStatus(_componentUid, "power.state", "off");
                _componentRegistryService.SetComponentStatus(_componentUid, "power.consumption", 0);
                return new WirehomeDictionary().WithType(ControlType.Success);
            }

            return adapterResult;
        }

        private WirehomeDictionary MoveUp()
        {
            var adapterResult = _adapter.SendMessage(new WirehomeDictionary().WithType("move_up"));
            if (adapterResult.IsSuccess())
            {
                _componentRegistryService.SetComponentStatus(_componentUid, "roller_shutter.state", "moving_up");
                //_componentRegistryService.SetComponentProperty(_componentUid, "roller_shutter.position", "unknown");
                _componentRegistryService.SetComponentStatus(_componentUid, "power.state", "on");
                _componentRegistryService.SetComponentStatus(_componentUid, "power.consumption", 0); // TODO: Get from adapter?
                return new WirehomeDictionary().WithType(ControlType.Success);
            }

            return adapterResult;
        }

        private WirehomeDictionary MoveDown()
        {
            var adapterResult = _adapter.SendMessage(new WirehomeDictionary().WithType("move_down"));
            if (adapterResult.IsSuccess())
            { 
                _componentRegistryService.SetComponentStatus(_componentUid, "roller_shutter.state", "moving_down");
                //_componentRegistryService.SetComponentProperty(_componentUid, "roller_shutter.position", "unknown");
                _componentRegistryService.SetComponentStatus(_componentUid, "power.state", "on");
                _componentRegistryService.SetComponentStatus(_componentUid, "power.consumption", 0); // TODO: Get from adapter?
                return new WirehomeDictionary().WithType(ControlType.Success);
            }

            return adapterResult;
        }
    }
}
