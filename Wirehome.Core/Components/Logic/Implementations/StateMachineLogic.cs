using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components.Adapters;
using Wirehome.Core.Constants;
using Wirehome.Core.Model;

namespace Wirehome.Core.Components.Logic.Implementations
{
    public class StateMachineLogic : IComponentLogic
    {
        private readonly ComponentRegistryService _componentRegistryService;
        private readonly string _componentUid;
        private readonly IComponentAdapter _adapter;
        private readonly ILogger _logger;

        public StateMachineLogic(string componentUid, IComponentAdapter adapter, ComponentRegistryService componentRegistryService, ILoggerFactory loggerFactory)
        {
            _componentUid = componentUid ?? throw new ArgumentNullException(nameof(componentUid));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<StateMachineLogic>();

            //adapter.MessageReceived += OnAdapterMessageReceived;
        }

        public WirehomeDictionary ExecuteCommand(WirehomeDictionary parameters)
        {
            if (parameters.IsOfType(ControlType.Initialize))
            {
                return Initialize();
            }

            if (parameters.IsOfType("turn_off"))
            {
                return SetState("off");
            }

            if (parameters.IsOfType("turn_on"))
            {
                return SetState("on");
            }

            if (parameters.IsOfType("set_state"))
            {
                if (!parameters.TryGetValue("state", out var state))
                {
                    return new WirehomeDictionary().WithType(ControlType.ParameterMissingException);
                }

                return SetState(state as string);
            }

            return new WirehomeDictionary().WithType(ControlType.NotSupportedException);
        }

        private WirehomeDictionary SetState(string id)
        {
            var adapterResult = _adapter.SendMessage(new WirehomeDictionary
            {
                ["type"] = "set_state",
                ["state"] = id
            });

            if (adapterResult.IsSuccess())
            {
                _componentRegistryService.SetComponentStatus(_componentUid, "state_machine.state", id);

                return new WirehomeDictionary().WithType(ControlType.Success);
            }

            return adapterResult;
        }

        private WirehomeDictionary Initialize()
        {
            _componentRegistryService.SetComponentStatus(_componentUid, "state_machine.state", "unknown");
            _componentRegistryService.SetConfiguration(_componentUid, "state_machine.states", "unknown");

            var adapterResult = _adapter.SendMessage(new WirehomeDictionary().WithType(ControlType.Initialize));
            if (adapterResult.IsSuccess())
            {
                _componentRegistryService.SetComponentStatus(_componentUid, "state_machine.state", adapterResult["state"]);

                var states = adapterResult["states"] as List<object>;

                _componentRegistryService.SetConfiguration(_componentUid, "state_machine.states", states);

                return new WirehomeDictionary().WithType(ControlType.Success);
            }

            return adapterResult;
        }
    }
}
