using System;
using Microsoft.Extensions.Logging;

namespace Wirehome.Core.Hardware.GPIO.Adapters
{
    public class TestGpioAdapter : IGpioAdapter
    {
        private readonly ILogger _logger;

        public TestGpioAdapter(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            _logger = loggerFactory.CreateLogger<TestGpioAdapter>();
        }

        public event EventHandler<GpioAdapterStateChangedEventArgs> GpioStateChanged;

        public void SetDirection(int gpio, GpioDirection direction)
        {
            //_logger.Log(LogLevel.Information, $"FAKE SetDirection: GPIO = {gpio}; Direction = {direction}");
        }

        public void WriteState(int gpio, GpioState state)
        {
            //_logger.Log(LogLevel.Information, $"FAKE SetState: GPIO = {gpio}; State = {state}");
        }

        public GpioState ReadState(int gpio)
        {
            //_logger.Log(LogLevel.Information, $"FAKE GetState: GPIO = {gpio}");

            return GpioState.Low;
        }

        public void EnableInterrupt(int gpio, GpioInterruptEdge edge)
        {
            //_logger.Log(LogLevel.Information, $"FAKE EnableInterrupt: GPIO = {gpio}; Edge = {edge}");
        }
    }
}
