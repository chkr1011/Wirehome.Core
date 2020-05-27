using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Exceptions;
using Wirehome.Core.Hardware.GPIO.Adapters;
using Wirehome.Core.MessageBus;

namespace Wirehome.Core.Hardware.GPIO
{
    public sealed class GpioRegistryService : WirehomeCoreService
    {
        readonly Dictionary<string, IGpioAdapter> _adapters = new Dictionary<string, IGpioAdapter>();
        readonly SystemStatusService _systemStatusService;
        readonly MessageBusService _messageBusService;
        readonly ILogger _logger;

        public GpioRegistryService(SystemStatusService systemStatusService, MessageBusService messageBusService, ILogger<GpioRegistryService> logger)
        {
            _systemStatusService = systemStatusService ?? throw new ArgumentNullException(nameof(systemStatusService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void RegisterAdapter(string hostId, IGpioAdapter adapter)
        {
            if (hostId == null) throw new ArgumentNullException(nameof(hostId));

            _adapters[hostId] = adapter ?? throw new ArgumentNullException(nameof(adapter));
            adapter.GpioStateChanged += (s, e) => OnGpioStateChanged(hostId, e);

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

        protected override void OnStart()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var gpioAdapter = new LinuxGpioAdapter(_systemStatusService, _logger);
                gpioAdapter.Enable();
                RegisterAdapter(string.Empty, gpioAdapter);
            }
            else
            {
                var gpioAdapter = new TestGpioAdapter(_logger);
                RegisterAdapter(string.Empty, gpioAdapter);
            }
        }

        IGpioAdapter GetAdapter(string hostId)
        {
            if (!_adapters.TryGetValue(hostId, out var adapter))
            {
                throw new ConfigurationException($"GPIO adapter '{hostId}' not registered.");
            }

            return adapter;
        }

        void OnGpioStateChanged(string hostId, GpioAdapterStateChangedEventArgs e)
        {
            var properties = new Dictionary<object, object>
            {
                ["type"] = "gpio_registry.event.state_changed",
                ["gpio_host_id"] = hostId,
                ["gpio_id"] = e.GpioId,
                ["old_state"] = e.OldState?.ToString().ToLowerInvariant(),
                ["new_state"] = e.NewState.ToString().ToLowerInvariant()
            };

            _messageBusService.Publish(properties);
        }
    }
}
