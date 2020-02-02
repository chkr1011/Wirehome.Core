using System;
using System.IO;

namespace Wirehome.Core.Hardware.GPIO.Adapters
{
    public class InterruptMonitor
    {
        public GpioState LatestState { get; set; }

        public DateTime Timestamp { get; set; }

        public string ValuePath { get; set; }

        public FileStream ValueFile { get; set; }
    }
}
