#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using IronPython.Runtime;
using Rssdp;
using System;
using System.Linq;
using Wirehome.Core.Extensions;
using Wirehome.Core.Python;

namespace Wirehome.Core.Discovery
{
    public class DiscoveryServicePythonProxy : IInjectedPythonProxy
    {
        readonly DiscoveryService _discoveryService;

        public DiscoveryServicePythonProxy(DiscoveryService discoveryService)
        {
            _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
        }

        public string ModuleName { get; } = "discovery";

        public List get_ssdp_devices()
        {
            var devices = _discoveryService.GetDiscoveredDevices().Select(ToPythonDictionary);
            return PythonConvert.ToPythonList(devices);
        }

        PythonDictionary ToPythonDictionary(DiscoveredSsdpDevice device)
        {
            var headersWrapper = new List();
            foreach (var header in device.ResponseHeaders)
            {
                if (header.Value == null)
                {
                    continue;
                }

                headersWrapper.Add(new PythonDictionary().WithValue("key", header.Key).WithValue("value", header.Value.FirstOrDefault()));
            }

            var deviceInfo = device.GetDeviceInfo().GetAwaiter().GetResult();

            var servicesWrapper = new List();
            foreach (var service in deviceInfo.Services)
            {
                servicesWrapper.Add(new PythonDictionary()
                    .WithValue("control_url", service.ControlUrl.ToString())
                    .WithValue("event_sub_url", service.EventSubUrl.ToString())
                    .WithValue("full_service_type", service.FullServiceType)
                    .WithValue("scpd_url", service.ScpdUrl.ToString())
                    .WithValue("service_id", service.ServiceId)
                    .WithValue("service_type", service.ServiceType)
                    .WithValue("service_type_namespace", service.ServiceTypeNamespace)
                    .WithValue("service_version", service.ServiceVersion)
                    .WithValue("uuid", service.Uuid));
            }

            return new PythonDictionary()
                .WithValue("usn", device.Usn)
                .WithValue("as_at", device.AsAt.ToString("O"))
                .WithValue("description_location", device.DescriptionLocation.ToString())
                .WithValue("cache_lifetime", device.CacheLifetime.ToString("c"))
                .WithValue("notification_type", device.NotificationType)
                .WithValue("response_headers", headersWrapper)
                .WithValue("device_type", deviceInfo.DeviceType)
                .WithValue("device_type_namespace", deviceInfo.DeviceTypeNamespace)
                .WithValue("friendly_name", deviceInfo.FriendlyName)
                .WithValue("full_device_type", deviceInfo.FullDeviceType)
                .WithValue("device_version", deviceInfo.DeviceVersion)
                .WithValue("serial_number", deviceInfo.SerialNumber)
                .WithValue("udn", deviceInfo.Udn)
                .WithValue("upc", deviceInfo.Upc)
                .WithValue("uuid", deviceInfo.Uuid)
                .WithValue("manufacturer", deviceInfo.Manufacturer)
                .WithValue("manufacturer_url", deviceInfo.ManufacturerUrl.ToString())
                .WithValue("model_description", deviceInfo.ModelDescription)
                .WithValue("model_name", deviceInfo.ModelName)
                .WithValue("model_number", deviceInfo.ModelNumber)
                .WithValue("services", servicesWrapper);
        }
    }
}
