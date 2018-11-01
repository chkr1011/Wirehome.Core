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
    public class DiscoveryServiceOptions
    {
        public const string Filename = "DiscoveryServiceConfiguration.json";

        public TimeSpan SearchDuration { get; set; } = TimeSpan.FromSeconds(10);
    }

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

                    var tasks = new List<Task>();
                    foreach (var device in devices)
                    {
                        tasks.Add(device.GetDeviceInfo());
                    }

                    await Task.WhenAll(tasks);
                    
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
