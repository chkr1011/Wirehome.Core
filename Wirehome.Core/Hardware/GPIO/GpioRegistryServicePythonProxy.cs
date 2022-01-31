#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using System.Collections.Concurrent;
using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Hardware.GPIO.Adapters;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Exceptions;

namespace Wirehome.Core.Hardware.GPIO
{
    public sealed class GpioRegistryServicePythonProxy : IInjectedPythonProxy
    {
        readonly GpioRegistryService _gpioRegistryService;
        readonly ConcurrentDictionary<string, Interrupt> _interrupts = new();
        readonly ILogger _logger;

        public GpioRegistryServicePythonProxy(GpioRegistryService gpioRegistryService, ILogger<GpioRegistryServicePythonProxy> logger)
        {
            _gpioRegistryService = gpioRegistryService ?? throw new ArgumentNullException(nameof(gpioRegistryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _gpioRegistryService.GpioStateChanged += OnGpioStateChanged;
        }

        public string ModuleName { get; } = "gpio";

        public string attach_interrupt(string uid, string gpio_host_id, int gpio_id, string @event, Action<PythonDictionary> callback)
        {
            if (gpio_host_id == null)
            {
                throw new ArgumentNullException(nameof(gpio_host_id));
            }

            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }

            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (string.IsNullOrEmpty(uid))
            {
                uid = Guid.NewGuid().ToString("D");
            }

            var interruptEvent = InterruptEvent.Rising;
            if (@event == "falling")
            {
                interruptEvent = InterruptEvent.Falling;
            }

            var interrupt = new Interrupt { GpioHostId = gpio_host_id, GpioId = gpio_id, Event = interruptEvent, Callback = callback };

            lock (_interrupts)
            {
                _interrupts[uid] = interrupt;
            }

            _gpioRegistryService.EnableInterrupt(gpio_host_id, gpio_id, GpioInterruptEdge.Both);

            return uid;
        }

        public void detach_interrupt(string uid)
        {
            if (uid == null)
            {
                throw new ArgumentNullException(nameof(uid));
            }

            lock (_interrupts)
            {
                _interrupts.TryRemove(uid, out _);
            }
        }

        public void enable_interrupt(string gpioHostId, int gpioId)
        {
            _gpioRegistryService.EnableInterrupt(gpioHostId, gpioId, GpioInterruptEdge.Both);
            _logger.Log(LogLevel.Information, $"Enabled interrupt for GPIO {gpioId}@'{gpioHostId}'.");
        }

        public string read_state(string host_id, int gpio_id)
        {
            var state = _gpioRegistryService.ReadState(host_id, gpio_id);

            return state.ToString().ToLowerInvariant();
        }

        public void set_direction(string hostId, int gpioId, string direction)
        {
            if (direction is null)
            {
                throw new ArgumentNullException(nameof(direction));
            }

            direction = direction.ToUpperInvariant();

            GpioDirection directionValue;
            switch (direction)
            {
                case "O":
                case "OUT":
                case "OUTPUT":
                    directionValue = GpioDirection.Output;
                    break;

                case "I":
                case "IN":
                case "INPUT":
                    directionValue = GpioDirection.Input;
                    break;

                default:
                    throw new PythonProxyException($"Unable to parse '{direction}' to a valid GPIO direction.");
            }

            _gpioRegistryService.SetDirection(hostId, gpioId, directionValue);
        }

        public void write_state(string host_id, int gpio_id, string state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            state = state.ToUpperInvariant();

            GpioState stateValue;
            switch (state)
            {
                case "LOW":
                case "0":
                    stateValue = GpioState.Low;
                    break;

                case "HIGH":
                case "1":
                    stateValue = GpioState.High;
                    break;

                default:
                    throw new PythonProxyException($"Unable to parse '{state}' to a valid GPIO state.");
            }

            _gpioRegistryService.WriteState(host_id, gpio_id, stateValue);
        }

        void OnGpioStateChanged(object sender, GpioStateChangedEventArgs args)
        {
            foreach (var interrupt in _interrupts)
            {
                if (interrupt.Value.GpioId != args.GpioId)
                {
                    continue;
                }

                if (args.NewState == GpioState.High)
                {
                    if (interrupt.Value.Event != InterruptEvent.Rising)
                    {
                        continue;
                    }
                }

                if (args.NewState == GpioState.Low)
                {
                    if (interrupt.Value.Event != InterruptEvent.Falling)
                    {
                        continue;
                    }
                }

                if (interrupt.Value.GpioHostId != args.HostId)
                {
                    continue;
                }

                var message = new PythonDictionary { ["uid"] = interrupt.Key, ["gpio_host_id"] = interrupt.Value.GpioHostId, ["gpio_id"] = interrupt.Value.GpioId, ["event"] = "falling" };
                if (interrupt.Value.Event == InterruptEvent.Rising)
                {
                    message["event"] = "rising";
                }

                interrupt.Value.Callback.Invoke(message);
            }
        }
    }
}