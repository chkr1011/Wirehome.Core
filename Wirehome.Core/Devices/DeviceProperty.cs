using System;

namespace Wirehome.Core.Devices;

public class DeviceProperty
{
    public DeviceProperty(object value, DateTime timestamp)
    {
        Value = value;
        Timestamp = timestamp;
    }

    public DateTime Timestamp { get; }

    public object Value { get; }
}