using System;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components.Adapters;
using Wirehome.Core.Constants;
using Wirehome.Core.Model;

namespace Wirehome.Core.Components.Logic.Implementations
{
    public class VentilationLogic : IComponentLogic
    {
        private readonly ComponentRegistryService _componentRegistryService;
        private readonly string _componentUid;
        private readonly IComponentAdapter _adapter;
        private readonly ILogger _logger;

        public VentilationLogic(string componentUid, IComponentAdapter adapter, ComponentRegistryService componentRegistryService, ILoggerFactory loggerFactory)
        {
            _componentUid = componentUid ?? throw new ArgumentNullException(nameof(componentUid));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _componentRegistryService = componentRegistryService;

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<LampLogic>();

            //adapter.MessageReceived += OnAdapterMessageReceived;
        }

        public WirehomeDictionary ExecuteCommand(WirehomeDictionary parameters)
        {
            var type = parameters["type"] as string;

            if (type == "initialize")
            {
                return Initialize();
            }

            if (type == "turn_off")
            {
                return SetLevel(0);
            }

            if (type == "set_level")
            {
                if (!parameters.ContainsKey("level"))
                {
                    return new WirehomeDictionary().WithType(ControlType.ParameterMissingException);
                }

                var level = Convert.ToInt32(parameters["level"]);
                return SetLevel(level);
            }

            if (type == "increase_level")
            {
                var level = Convert.ToInt32(_componentRegistryService.GetComponentStatus(_componentUid, "level.current"));
                var maxLevel = Convert.ToInt32(_componentRegistryService.GetComponentConfiguration(_componentUid, "level.max"));

                level++;

                if (level > maxLevel)
                {
                    level = 0;
                }
                
                return SetLevel(level);
            }

            if (type == "decrease_level")
            {
                var level = Convert.ToInt32(_componentRegistryService.GetComponentStatus(_componentUid, "level.current"));
                level--;

                if (level < 0)
                {
                    level = Convert.ToInt32(_componentRegistryService.GetComponentConfiguration(_componentUid, "level.max"));
                }

                return SetLevel(level);
            }

            return new WirehomeDictionary().WithType(ControlType.NotSupportedException);
        }

        private WirehomeDictionary SetLevel(long level)
        {
            var adapterResult = _adapter.SendMessage(new WirehomeDictionary
            {
                ["type"] = "set_level",
                ["level"] = level
            });

            if (adapterResult.IsSuccess())
            {
                _componentRegistryService.SetComponentStatus(_componentUid, "level.current", level);
            }
            else
            {
                return adapterResult;
            }

            return new WirehomeDictionary().WithType(ControlType.Success);
        }

        private WirehomeDictionary Initialize()
        {
            _componentRegistryService.SetComponentStatus(_componentUid, "level.current", "unknown");

            _componentRegistryService.SetConfiguration(_componentUid, "level.max", "unknown");

            var adapterResult = _adapter.SendMessage(new WirehomeDictionary().WithType(ControlType.Initialize));

            if (adapterResult.IsSuccess())
            {
                var maxLevel = Convert.ToInt32(adapterResult["level.max"]);

                _componentRegistryService.SetConfiguration(_componentUid, "level.max", maxLevel);
                return SetLevel(0);
            }

            return adapterResult;
        }
    }
}
