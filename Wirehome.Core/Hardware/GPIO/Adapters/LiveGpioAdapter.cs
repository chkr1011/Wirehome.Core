using System;

namespace Wirehome.Core.Hardware.GPIO.Adapters
{
    public class LiveGpioAdapter : IGpioAdapter
    {
        public event EventHandler<GpioAdapterStateChangedEventArgs> GpioStateChanged;

        public void EnableInterrupt(int gpioId, GpioInterruptEdge edge)
        {
            throw new NotImplementedException();
        }

        public GpioState ReadState(int gpioId)
        {
            throw new NotImplementedException();
        }

        public void SetDirection(int gpioId, GpioDirection direction)
        {
            throw new NotImplementedException();
        }

        public void WriteState(int gpioId, GpioState state)
        {
            throw new NotImplementedException();
        }
    }
}
