using System;

namespace Wirehome.Core.Hardware.GPIO.Adapters
{
    public class GpioAdapterStateChangedEventArgs : EventArgs
    {
        public int GpioId { get; set; }

        public GpioState? OldState { get; set; }

        public GpioState NewState { get; set; }
    }
}