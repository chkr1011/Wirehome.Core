using System;

namespace Wirehome.Core.Hardware.GPIO.Adapters
{
    public interface IGpioAdapter
    {
        event EventHandler<GpioAdapterStateChangedEventArgs> GpioStateChanged;

        void SetDirection(int gpioId, GpioDirection direction);

        void WriteState(int gpioId, GpioState state);

        GpioState ReadState(int gpioId);

        void EnableInterrupt(int gpioId, GpioInterruptEdge edge);
    }
}