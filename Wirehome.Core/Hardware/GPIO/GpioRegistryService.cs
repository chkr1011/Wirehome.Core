using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Exceptions;
using Wirehome.Core.Hardware.GPIO.Adapters;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Model;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Proxies;

namespace Wirehome.Core.Hardware.GPIO
{
    public class GpioRegistryService
    {
        private readonly Dictionary<string, IGpioAdapter> _adapters = new Dictionary<string, IGpioAdapter>();
        private readonly MessageBusService _messageBusService;
        private readonly ILogger _logger;

        public GpioRegistryService(PythonEngineService pythonEngineService, MessageBusService messageBusService, ILoggerFactory loggerFactory)
        {
            if (pythonEngineService == null) throw new ArgumentNullException(nameof(pythonEngineService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));

            _logger = loggerFactory?.CreateLogger<GpioRegistryService>() ?? throw new ArgumentNullException(nameof(loggerFactory));

            pythonEngineService.RegisterSingletonProxy(new GpioPythonProxy(this, loggerFactory));
        }

        public void RegisterAdapter(string hostId, IGpioAdapter adapter)
        {
            if (hostId == null) throw new ArgumentNullException(nameof(hostId));

            _adapters[hostId] = adapter ?? throw new ArgumentNullException(nameof(adapter));
            adapter.GpioStateChanged += (s, e) => DispatchGpioStateChangedEvent(hostId, e.GpioId, e.OldState, e.NewState);

            _logger.Log(LogLevel.Information, $"Registered GPIO host '{hostId}'.");
        }

        public void SetDirection(string hostId, int gpioId, GpioDirection direction)
        {
            if (hostId == null) throw new ArgumentNullException(nameof(hostId));

            GetAdapter(hostId).SetDirection(gpioId, direction);
        }

        public void WriteState(string hostId, int gpioId, GpioState state)
        {
            if (hostId == null) throw new ArgumentNullException(nameof(hostId));

            GetAdapter(hostId).WriteState(gpioId, state);
        }

        public GpioState ReadState(string hostId, int gpioId)
        {
            if (hostId == null) throw new ArgumentNullException(nameof(hostId));

            return GetAdapter(hostId).ReadState(gpioId);
        }

        public void EnableInterrupt(string hostId, int gpioId, GpioInterruptEdge edge)
        {
            if (hostId == null) throw new ArgumentNullException(nameof(hostId));

            GetAdapter(hostId).EnableInterrupt(gpioId, edge);
        }

        private IGpioAdapter GetAdapter(string hostId)
        {
            if (!_adapters.TryGetValue(hostId, out var adapter))
            {
                throw new WirehomeConfigurationException($"GPIO adapter '{hostId}' not registered.");
            }

            return adapter;
        }

        private void DispatchGpioStateChangedEvent(string gpioHostId, int gpioId, GpioState oldState, GpioState newState)
        {
            var properties = new WirehomeDictionary
            {
                ["type"] = "gpio_registry.event.state_changed",
                ["gpio_host_id"] = gpioHostId,
                ["gpio_id"] = gpioId,
                ["old_state"] = oldState.ToString().ToLowerInvariant(),
                ["new_state"] = newState.ToString().ToLowerInvariant()
            };

            _messageBusService.Publish(properties);
        }
    }
}
