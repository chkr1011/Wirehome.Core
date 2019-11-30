#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using Wirehome.Core.Python;

namespace Wirehome.Core.Devices
{
    public class DeviceRegistryServicePythonProxy : IInjectedPythonProxy
    {
        readonly DeviceRegistryService _deviceRegistryService;

        public DeviceRegistryServicePythonProxy(DeviceRegistryService deviceRegistryService)
        {
            _deviceRegistryService = deviceRegistryService ?? throw new ArgumentNullException(nameof(deviceRegistryService));
        }

        public string ModuleName { get; } = "devices";

        public void report_property(string device_uid, string property_uid, object value)
        {
            _deviceRegistryService.ReportProperty(device_uid, property_uid, value).GetAwaiter().GetResult();
        }
    }
}