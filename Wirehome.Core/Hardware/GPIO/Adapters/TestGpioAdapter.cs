using Microsoft.Extensions.Logging;
using System;

namespace Wirehome.Core.Hardware.GPIO.Adapters
{
    public class TestGpioAdapter : IGpioAdapter
    {
        private readonly ILogger _logger;

        public TestGpioAdapter(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public event EventHandler<GpioAdapterStateChangedEventArgs> GpioStateChanged;

        public void NotifyGpioStateChanged(int gpioId, GpioState oldState, GpioState newState)
        {
            GpioStateChanged?.Invoke(this, new GpioAdapterStateChangedEventArgs
            {
                GpioId = gpioId,
                OldState = oldState,
                NewState = newState
            });
        }

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
