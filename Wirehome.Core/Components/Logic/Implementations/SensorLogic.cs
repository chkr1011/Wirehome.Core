using System;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components.Adapters;
using Wirehome.Core.Constants;
using Wirehome.Core.Model;

namespace Wirehome.Core.Components.Logic.Implementations
{
    public class SensorLogic : IComponentLogic
    {
        private readonly ComponentRegistryService _componentRegistryService;
        private readonly string _componentUid;
        private readonly string _valueType;
        private readonly IComponentAdapter _adapter;
        private readonly ILogger _logger;

        public SensorLogic(string componentUid, string valueType, IComponentAdapter adapter, ComponentRegistryService componentRegistryService, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            _componentUid = componentUid ?? throw new ArgumentNullException(nameof(componentUid));
            _valueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
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
            _componentRegistryService.SetComponentStatus(_componentUid, _valueType, "unknown");

            _adapter.SendMessage(new WirehomeDictionary().WithType(ControlType.Initialize));
        }

        private void OnAdapterMessageReceived(object sender, ComponentAdapterMessageReceivedEventArgs eventArgs)
        {
            var properties = eventArgs.Properties;

            if (properties.IsOfType("value_updated"))
            {
                var realValue = 0.0D;

                var value = properties["value"];
                var valueType = properties["value_type"] as string;

                if (valueType == "string")
                {
                    realValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                }

                _componentRegistryService.SetComponentStatus(_componentUid, _valueType, realValue);

                eventArgs.Result["type"] = ControlType.Success;
            }
        }
    }
}
