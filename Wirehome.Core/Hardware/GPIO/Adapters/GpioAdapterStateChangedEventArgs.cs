using System;

namespace Wirehome.Core.Hardware.GPIO.Adapters
{
    public class GpioAdapterStateChangedEventArgs : EventArgs
    {
        public GpioAdapterStateChangedEventArgs(int gpioId, GpioState oldState, GpioState newState)
        {
            GpioId = gpioId;
            OldState = oldState;
            NewState = newState;
        }

        public int GpioId { get; }

        public GpioState OldState { get; }

        public GpioState NewState { get; }
    }
}