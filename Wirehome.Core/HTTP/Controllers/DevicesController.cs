using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Devices;

namespace Wirehome.Core.HTTP.Controllers;

[ApiController]
public sealed class DevicesController : Controller
{
    readonly DeviceRegistryService _deviceRegistryService;

    public DevicesController(DeviceRegistryService deviceRegistryService)
    {
        _deviceRegistryService = deviceRegistryService ?? throw new ArgumentNullException(nameof(deviceRegistryService));
    }

    [HttpGet]
    [Route("api/v1/devices/{uid}/properties")]
    [ApiExplorerSettings(GroupName = "v1")]
    public async Task<object> GetDeviceProperties(string uid)
    {
        var device = await _deviceRegistryService.GetDevice(uid).ConfigureAwait(false);

        return new
        {
            Reported = device.GetReportedProperties(),
            Requested = device.GetRequestedProperties()
        };
    }

    [HttpGet]
    [Route("api/v1/devices/uids")]
    [ApiExplorerSettings(GroupName = "v1")]
    public Task<List<string>> GetDeviceUids()
    {
        return _deviceRegistryService.GetDeviceUids();
    }

    [HttpGet]
    [Route("api/v1/devices/{uid}/properties/reported")]
    [ApiExplorerSettings(GroupName = "v1")]
    public async Task<Dictionary<string, DeviceProperty>> GetReportedDeviceProperties(string uid)
    {
        return (await _deviceRegistryService.GetDevice(uid).ConfigureAwait(false)).GetReportedProperties();
    }

    [HttpGet]
    [Route("api/v1/devices/{deviceUid}/properties/reported/{propertyUid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public object GetReportedDeviceProperty(string deviceUid, string propertyUid)
    {
        throw new NotSupportedException();
    }

    [HttpGet]
    [Route("api/v1/devices/{uid}/properties/requested")]
    [ApiExplorerSettings(GroupName = "v1")]
    public async Task<Dictionary<string, DeviceProperty>> GetRequestedDeviceProperties(string uid)
    {
        return (await _deviceRegistryService.GetDevice(uid).ConfigureAwait(false)).GetRequestedProperties();
    }

    [HttpGet]
    [Route("api/v1/devices/{deviceUid}/properties/requested/{propertyUid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public List<string> GetRequestedDeviceProperty(string deviceUid, string propertyUid)
    {
        throw new NotImplementedException();
    }

    [HttpPost]
    [Route("api/v1/devices/{deviceUid}/properties/reported/{propertyUid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public Task PostReportedDeviceProperty(string deviceUid, string propertyUid, [FromBody] object value)
    {
        return _deviceRegistryService.ReportProperty(deviceUid, propertyUid, value);
    }

    [HttpPost]
    [Route("api/v1/devices/{deviceUid}/properties/requested/{propertyUid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public Task PostRequestedDeviceProperty(string deviceUid, string propertyUid, [FromBody] object value)
    {
        return _deviceRegistryService.RequestProperty(deviceUid, propertyUid, value);
    }
}