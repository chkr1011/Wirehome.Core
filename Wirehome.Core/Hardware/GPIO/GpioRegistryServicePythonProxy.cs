#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using Microsoft.Extensions.Logging;
using System;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Exceptions;

namespace Wirehome.Core.Hardware.GPIO
{
    public class DeviceRegistryServicePythonProxy : IInjectedPythonProxy
    {
        readonly ILogger _logger;
        readonly GpioRegistryService _gpioRegistryService;

        public DeviceRegistryServicePythonProxy(GpioRegistryService gpioRegistryService, ILogger<DeviceRegistryServicePythonProxy> logger)
        {
            _gpioRegistryService = gpioRegistryService ?? throw new ArgumentNullException(nameof(gpioRegistryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string ModuleName { get; } = "gpio";

        public void set_direction(string hostId, int gpioId, string direction)
        {
            if (direction is null) throw new ArgumentNullException(nameof(direction));

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

        public void write_state(string hostId, int gpioId, string state)
        {
            if (state is null) throw new ArgumentNullException(nameof(state));

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

            _gpioRegistryService.WriteState(hostId, gpioId, stateValue);
        }

        public string read_state(string hostId, int gpioId)
        {
            var state = _gpioRegistryService.ReadState(hostId, gpioId);

            return state.ToString().ToLowerInvariant();
        }

        public void enable_interrupt(string gpioHostId, int gpioId)
        {
            _gpioRegistryService.EnableInterrupt(gpioHostId, gpioId, GpioInterruptEdge.Both);
            _logger.Log(LogLevel.Information, $"Enabled interrupt for GPIO {gpioId}@'{gpioHostId}'.");
        }
    }
}