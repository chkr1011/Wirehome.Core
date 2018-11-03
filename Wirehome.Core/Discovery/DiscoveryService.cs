using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rssdp;
using Wirehome.Core.Extensions;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Proxies;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Discovery
{
    // TODO: Create own model with all information in place.
    public class DiscoveryService
    {
        private readonly List<DiscoveredSsdpDevice> _discoveredSsdpDevices = new List<DiscoveredSsdpDevice>();
        private readonly ILogger _logger;
        private readonly DiscoveryServiceOptions _options;

        private SsdpDevicePublisher _publisher;
        
        public DiscoveryService(PythonEngineService pythonEngineService, StorageService storageService, ILoggerFactory loggerFactory)
        {
            if (pythonEngineService == null) throw new ArgumentNullException(nameof(pythonEngineService));
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            _logger = loggerFactory.CreateLogger<DiscoveryService>();

            pythonEngineService.RegisterSingletonProxy(new DiscoveryPythonProxy(this));

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
                await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
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
                        try
                        {
                            await device.GetDeviceInfo().ConfigureAwait(false);
                        }
                        catch (Exception exception)
                        {
                            _logger.LogWarning(exception, $"Error while loading device info from '{device.DescriptionLocation}.'");
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
