using System;

namespace Wirehome.Core.Hardware.GPIO.Adapters
{
    public interface IGpioAdapter
    {
        event EventHandler<GpioAdapterStateChangedEventArgs> GpioStateChanged;

        void EnableInterrupt(int gpioId, GpioInterruptEdge edge);

        GpioState ReadState(int gpioId);

        void SetDirection(int gpioId, GpioDirection direction);

        void WriteState(int gpioId, GpioState state);
    }
}