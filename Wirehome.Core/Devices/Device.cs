using System;
using System.Collections.Generic;

namespace Wirehome.Core.Devices;

public class Device
{
    readonly Dictionary<string, DeviceProperty> _reportedProperties = new();
    readonly Dictionary<string, DeviceProperty> _requestedProperties = new();

    public DateTime? FirstSeen { get; set; }

    public DateTime LastUpdate { get; set; }

    public Dictionary<string, DeviceProperty> GetReportedProperties()
    {
        lock (_reportedProperties)
        {
            return new Dictionary<string, DeviceProperty>(_reportedProperties);
        }
    }

    public Dictionary<string, DeviceProperty> GetRequestedProperties()
    {
        lock (_requestedProperties)
        {
            return new Dictionary<string, DeviceProperty>(_requestedProperties);
        }
    }

    public void SetReportedProperty(string uid, object value, DateTime timestamp)
    {
        if (uid is null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        lock (_reportedProperties)
        {
            SetProperty(uid, value, timestamp, _reportedProperties);
        }
    }

    public void SetRequestedProperty(string uid, object value, DateTime timestamp)
    {
        lock (_requestedProperties)
        {
            SetProperty(uid, value, timestamp, _requestedProperties);
        }
    }

    void SetProperty(string uid, object value, DateTime timestamp, Dictionary<string, DeviceProperty> target)
    {
        if (uid is null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        target[uid] = new DeviceProperty(value, timestamp);
        LastUpdate = timestamp;
    }
}