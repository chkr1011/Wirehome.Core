using System;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components.Adapters;
using Wirehome.Core.Constants;
using Wirehome.Core.Model;

namespace Wirehome.Core.Components.Logic.Implementations
{
    public class MotionDetectorLogic : IComponentLogic
    {
        private readonly ComponentRegistryService _componentRegistryService;
        private readonly string _componentUid;
        private readonly IComponentAdapter _adapter;
        private readonly ILogger _logger;

        public MotionDetectorLogic(string componentUid, IComponentAdapter adapter, ComponentRegistryService componentRegistryService, ILoggerFactory loggerFactory)
        {
            _componentUid = componentUid ?? throw new ArgumentNullException(nameof(componentUid));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _componentRegistryService = componentRegistryService;

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<LampLogic>();

            adapter.MessageReceived += OnAdapterMessageReceived;
        }

        public WirehomeDictionary ExecuteCommand(WirehomeDictionary parameters)
        {
            if (parameters.IsOfType("initialize"))
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
            _componentRegistryService.SetComponentStatus(_componentUid, "motion_detection.state", "unknown");
            _adapter.SendMessage(new WirehomeDictionary().WithType(ControlType.Initialize));
        }

        private void OnAdapterMessageReceived(object sender, ComponentAdapterMessageReceivedEventArgs eventArgs)
        {
            var properties = eventArgs.Properties;

            var type = properties["type"] as string;
            if (type == "state_changed")
            {
                var state = properties["new_state"] as string;

                if (state == "idle")
                {
                    _componentRegistryService.SetComponentStatus(_componentUid, "motion_detection.state", "idle");
                }
                else if (state == "detected")
                {
                    _componentRegistryService.SetComponentStatus(_componentUid, "motion_detection.state", "detected");
                }
            }
        }
    }
}
