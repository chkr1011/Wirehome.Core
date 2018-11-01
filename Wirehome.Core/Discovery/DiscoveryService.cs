using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rssdp;
using Wirehome.Core.Extensions;

namespace Wirehome.Core.Discovery
{
    public class DiscoveryService
    {
        private readonly List<DiscoveredSsdpDevice> _discoveredSsdpDevices = new List<DiscoveredSsdpDevice>();
        private readonly ILogger _logger;

        private SsdpDevicePublisher _publisher;
        
        public DiscoveryService(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            _logger = loggerFactory.CreateLogger<DiscoveryService>();
        }

        public void Start()
        {
            _publisher = new SsdpDevicePublisher();

            Task.Run(SearchAsync).Forget(_logger);
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
                    var discoverResult = new List<DiscoveredSsdpDevice>(await deviceLocator.SearchAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false));

                    lock (_discoveredSsdpDevices)
                    {
                        // TODO: Consider adding var fullDevice = await foundDevice.GetDeviceInfo();

                        _discoveredSsdpDevices.Clear();
                        _discoveredSsdpDevices.AddRange(discoverResult);

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
