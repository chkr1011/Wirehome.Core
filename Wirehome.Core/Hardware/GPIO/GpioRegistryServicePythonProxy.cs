#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Exceptions;

namespace Wirehome.Core.Hardware.GPIO
{
    public sealed class GpioRegistryServicePythonProxy : IInjectedPythonProxy
    {
        readonly GpioRegistryService _gpioRegistryService;
        readonly ILogger _logger;

        public GpioRegistryServicePythonProxy(GpioRegistryService gpioRegistryService, ILogger<GpioRegistryServicePythonProxy> logger)
        {
            _gpioRegistryService = gpioRegistryService ?? throw new ArgumentNullException(nameof(gpioRegistryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string ModuleName { get; } = "gpio";

        public void attach(string uid, string host_id, int gpio_id, string @event, Action<PythonDictionary> callback)
        {
            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            _gpioRegistryService.GpioStateChanged += (sender, args) =>
            {
                if (args.GpioId != gpio_id)
                {
                    return;
                }

                if (args.NewState == GpioState.High)
                {
                    if (@event != "rising")
                    {
                        return;
                    }
                }

                if (args.NewState == GpioState.Low)
                {
                    if (@event != "falling")
                    {
                        return;
                    }
                }

                callback.Invoke(new PythonDictionary
                {
                    ["uid"] = uid,
                    ["host_id"] = host_id,
                    ["gpio_id"] = gpio_id,
                    ["event"] = @event
                });
            };
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
    }
}