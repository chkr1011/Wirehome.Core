using System;

namespace Wirehome.Core.Hardware.GPIO.Adapters
{
    public sealed class GpioAdapterStateChangedEventArgs : EventArgs
    {
        public int GpioId { get; set; }

        public GpioState NewState { get; set; }

        public GpioState? OldState { get; set; }
    }
}