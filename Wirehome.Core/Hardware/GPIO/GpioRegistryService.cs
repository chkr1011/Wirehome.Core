using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Contracts;
using Wirehome.Core.Exceptions;
using Wirehome.Core.Hardware.GPIO.Adapters;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Model;

namespace Wirehome.Core.Hardware.GPIO
{
    public class GpioRegistryService : IService
    {
        private readonly Dictionary<string, IGpioAdapter> _adapters = new Dictionary<string, IGpioAdapter>();
        private readonly MessageBusService _messageBusService;
        private readonly ILogger _logger;

        public GpioRegistryService(MessageBusService messageBusService, ILogger<GpioRegistryService> logger)
        {
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Start()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var gpioAdapter = new LinuxGpioAdapter(_logger);
                gpioAdapter.Enable();
                RegisterAdapter(string.Empty, gpioAdapter);
            }
            else
            {
                var gpioAdapter = new TestGpioAdapter(_logger);
                RegisterAdapter(string.Empty, gpioAdapter);
            }
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
                throw new ConfigurationException($"GPIO adapter '{hostId}' not registered.");
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
