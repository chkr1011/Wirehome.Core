using System;

namespace Wirehome.Core.Hardware.GPIO.Adapters
{
    public sealed class InterruptMonitor
    {
        public GpioState? LatestState { get; set; }

        public DateTime Timestamp { get; set; }
    }
}