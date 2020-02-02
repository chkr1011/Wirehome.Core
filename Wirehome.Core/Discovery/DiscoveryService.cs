using Microsoft.Extensions.Logging;
using Rssdp;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wirehome.Core.Contracts;
using Wirehome.Core.Extensions;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Discovery
{
    // TODO: Create own model with all information in place.
    public class DiscoveryService : IService
    {
        private readonly List<DiscoveredSsdpDevice> _discoveredSsdpDevices = new List<DiscoveredSsdpDevice>();
        private readonly ILogger _logger;
        private readonly DiscoveryServiceOptions _options;

        private SsdpDevicePublisher _publisher;

        public DiscoveryService(StorageService storageService, ILogger<DiscoveryService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            storageService.TryReadOrCreate(out _options, DiscoveryServiceOptions.Filename);
        }

        public void Start()
        {
            _publisher = new SsdpDevicePublisher();

            //var rootDevice = new SsdpRootDevice
            //{
            //    Uuid = "c6faa85a-d7e9-48b7-8c54-7459c4d9c329",

            //    CacheLifetime = TimeSpan.Zero,
            //    //UrlBase = new Uri("http://localhost"),
            //    //PresentationUrl = new Uri("configurator", UriKind.Relative),
            //    FriendlyName = "Wirehome.Core",

            //    Manufacturer = "Wirehome",
            //    //ManufacturerUrl = new Uri("https://github.com/chkr1011/Wirehome.Core/"),

            //    ModelNumber = WirehomeCoreVersion.Version,
            //    //ModelUrl = new Uri("app", UriKind.Relative),
            //    ModelName = "Wirehome.Core",
            //    ModelDescription = "Wirehome.Core",
            //};

            //_publisher.AddDevice(rootDevice);

            ParallelTask.Start(SearchAsync, CancellationToken.None, _logger);
        }

        public List<DiscoveredSsdpDevice> GetDiscoveredDevices()
        {
            lock (_discoveredSsdpDevices)
            {
                return new List<DiscoveredSsdpDevice>(_discoveredSsdpDevices);
            }
        }

        private async Task SearchAsync()
        {
            while (true)
            {
                await TryDiscoverSsdpDevicesAsync().ConfigureAwait(false);

                await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            }
        }

        //public string AddDeviceDefinition(string uid)
        //{
        //    var device = new SsdpRootDevice();
        //    device.CacheLifetime = TimeSpan.MaxValue;
        //    device.Location = new Uri("");
        //    device.DeviceTypeNamespace = "";
        //    //device.DeviceType

        //}

        private async Task TryDiscoverSsdpDevicesAsync()
        {
            try
            {
                using (var deviceLocator = new SsdpDeviceLocator())
                {
                    var devices = new List<DiscoveredSsdpDevice>(await deviceLocator.SearchAsync(_options.SearchDuration).ConfigureAwait(false));
                    foreach (var device in devices)
                    {
                        if (Convert.ToString(device.DescriptionLocation).Contains("0.0.0.0"))
                        {
                            continue;
                        }

                        try
                        {
                            await device.GetDeviceInfo().ConfigureAwait(false);
                        }
                        catch (Exception exception)
                        {
                            _logger.LogDebug(exception, $"Error while loading device info from '{device.DescriptionLocation}.'");
                        }
                    }

                    lock (_discoveredSsdpDevices)
                    {
                        _discoveredSsdpDevices.Clear();
                        _discoveredSsdpDevices.AddRange(devices);

                        _logger.LogInformation($"Discovered {_discoveredSsdpDevices.Count} SSDP devices.");
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while discovering SSDP devices.");
            }
        }
    }
}
