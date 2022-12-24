using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using Newtonsoft.Json.Linq;
using Wirehome.Core.Contracts;
using Wirehome.Core.Devices.Exceptions;
using Wirehome.Core.Foundation;
using Wirehome.Core.Hardware.MQTT;

namespace Wirehome.Core.Devices;

public sealed class DeviceRegistryService : WirehomeCoreService
{
    readonly Dictionary<string, Device> _devices = new();
    readonly AsyncLock _devicesLock = new();
    readonly ILogger<DeviceRegistryService> _logger;

    readonly MqttService _mqttService;

    public DeviceRegistryService(MqttService mqttService, ILogger<DeviceRegistryService> logger)
    {
        _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _mqttService.Subscribe(null, "$wirehome/devices/+/report/+", OnPropertyReportedViaMqtt);
    }

    public async Task<Device> GetDevice(string uid)
    {
        await _devicesLock.EnterAsync().ConfigureAwait(false);
        try
        {
            return GetDeviceInternal(uid);
        }
        finally
        {
            _devicesLock.Exit();
        }
    }

    public async Task<List<string>> GetDeviceUids()
    {
        await _devicesLock.EnterAsync().ConfigureAwait(false);
        try
        {
            return new List<string>(_devices.Keys);
        }
        finally
        {
            _devicesLock.Exit();
        }
    }

    public async Task ReportProperty(string deviceUid, string propertyUid, object value)
    {
        if (deviceUid is null)
        {
            throw new ArgumentNullException(nameof(deviceUid));
        }

        if (propertyUid is null)
        {
            throw new ArgumentNullException(nameof(propertyUid));
        }

        await _devicesLock.EnterAsync().ConfigureAwait(false);
        try
        {
            var utcNow = DateTime.UtcNow;

            if (!_devices.TryGetValue(deviceUid, out var device))
            {
                device = new Device
                {
                    // Update FirstSeen only if the device has send some data.
                    FirstSeen = utcNow
                };

                _devices.Add(deviceUid, device);
            }

            device.SetReportedProperty(propertyUid, value, DateTime.UtcNow);

            _logger.LogInformation($"Device '{0}' reported property '{1}' with value '{2}'.", deviceUid, propertyUid, value);
        }
        finally
        {
            _devicesLock.Exit();
        }
    }

    public async Task RequestProperty(string deviceUid, string propertyUid, object value)
    {
        if (deviceUid is null)
        {
            throw new ArgumentNullException(nameof(deviceUid));
        }

        if (propertyUid is null)
        {
            throw new ArgumentNullException(nameof(propertyUid));
        }

        await _devicesLock.EnterAsync().ConfigureAwait(false);
        try
        {
            var utcNow = DateTime.UtcNow;

            if (!_devices.TryGetValue(deviceUid, out var device))
            {
                device = new Device();
                _devices.Add(deviceUid, device);
            }

            device.SetRequestedProperty(propertyUid, value, DateTime.UtcNow);

            _logger.LogInformation($"Property '{0}' with value '{1}' requested for device '{2}'.", propertyUid, value, deviceUid);
        }
        finally
        {
            _devicesLock.Exit();
        }
    }

    Device GetDeviceInternal(string uid)
    {
        if (!_devices.TryGetValue(uid, out var device))
        {
            throw new DeviceNotFoundException(uid);
        }

        return device;
    }

    void OnPropertyReportedViaMqtt(IncomingMqttMessage eventArgs)
    {
        // $wirehome/devices/+/report/+
        // 0        /1      /2/3     /4
        var fragments = eventArgs.ApplicationMessage.Topic.Split('/');

        var deviceUid = fragments[2];
        var propertyUid = fragments[4];
        var value = JToken.Parse(eventArgs.ApplicationMessage.ConvertPayloadToString()).ToObject<object>();

        ReportProperty(deviceUid, propertyUid, value).GetAwaiter().GetResult();
    }
}