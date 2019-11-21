using System;

namespace Wirehome.Core.Hardware.GPIO.Adapters
{
    public class InterruptMonitor
    {
        public GpioState LatestState;

        public DateTime Timestamp;

        public string GpioValuePath;
    }
}
