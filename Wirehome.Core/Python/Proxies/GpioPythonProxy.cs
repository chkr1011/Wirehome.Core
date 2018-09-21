#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Hardware.GPIO;
using Wirehome.Core.Python.Exceptions;

namespace Wirehome.Core.Python.Proxies
{
    public class GpioPythonProxy : IPythonProxy
    {
        private readonly ILogger _logger;
        private readonly GpioRegistryService _gpioRegistryService;

        public GpioPythonProxy(GpioRegistryService gpioRegistryService, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _gpioRegistryService = gpioRegistryService ?? throw new ArgumentNullException(nameof(gpioRegistryService));

            _logger = loggerFactory.CreateLogger<GpioPythonProxy>();
        }

        public string ModuleName { get; } = "gpio";

        public void set_direction(string hostId, int gpioId, string direction)
        {
            direction = direction.ToLowerInvariant();

            GpioDirection directionValue;
            switch (direction)
            {
                case "o":
                case "out":
                case "output":
                    directionValue = GpioDirection.Output;
                    break;

                case "i":
                case "in":
                case "input":
                    directionValue = GpioDirection.Input;
                    break;

                default:
                    throw new PythonProxyException($"Unable to parse '{direction}' to a valid GPIO direction.");
            }

            _gpioRegistryService.SetDirection(hostId, gpioId, directionValue);
        }

        public void write_state(string hostId, int gpioId, string state)
        {
            state = state.ToLowerInvariant();

            GpioState stateValue;
            switch (state)
            {
                case "low":
                case "0":
                    stateValue = GpioState.Low;
                    break;

                case "high":
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