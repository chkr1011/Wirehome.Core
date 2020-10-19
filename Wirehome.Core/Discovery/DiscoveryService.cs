using Microsoft.Extensions.Logging;
using Rssdp;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Wirehome.Core.Contracts;
using Wirehome.Core.Storage;
using Wirehome.Core.System;

namespace Wirehome.Core.Discovery
{
    public sealed class DiscoveryService : WirehomeCoreService
    {
        readonly List<SsdpDevice> _discoveredSsdpDevices = new List<SsdpDevice>();
        readonly SystemCancellationToken _systemCancellationToken;
        readonly ILogger _logger;
        readonly DiscoveryServiceOptions _options;
        readonly HttpClient _httpClient = new HttpClient();
        
        //SsdpDevicePublisher _publisher;

        public DiscoveryService(StorageService storageService, SystemCancellationToken systemCancellationToken, ILogger<DiscoveryService> logger)
        {
            _systemCancellationToken = systemCancellationToken ?? throw new ArgumentNullException(nameof(systemCancellationToken));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (storageService is null) throw new ArgumentNullException(nameof(storageService));
            storageService.SafeReadSerializedValue(out _options, DefaultDirectoryNames.Configuration, DiscoveryServiceOptions.Filename);
        }

        public List<SsdpDevice> GetDiscoveredDevices()
        {
            lock (_discoveredSsdpDevices)
            {
                return new List<SsdpDevice>(_discoveredSsdpDevices);
            }
        }

        protected override void OnStart()
        {
            //_publisher = new SsdpDevicePublisher();

            //var deviceDefinition = new SsdpRootDevice()
            //{
            //    CacheLifetime = TimeSpan.FromHours(1),
            //    Location = new Uri("http://wirehome.local/upnp.xml"), // TODO: Must point to the URL that serves your devices UPnP description document. 
            //    DeviceTypeNamespace = "my-namespace",
            //    DeviceType = "Wirehome.Core",
            //    FriendlyName = "Wirehome Core",
            //    Manufacturer = "Christian Kratky",
            //    ModelName = "Wirehome.Core",
            //    Uuid = "uuid:c6faa85a-d7e9-48b7-8c54-7459c4d9c329"
            //};

            //_publisher.AddDevice(deviceDefinition);

            //Task.Run(() => SearchAsync(_systemCancellationToken.Token), _systemCancellationToken.Token);
        }

        async Task SearchAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await TryDiscoverDevicesAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Error while discovering SSDP devices.");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        async Task TryDiscoverDevicesAsync(CancellationToken cancellationToken)
        {
            using (var deviceLocator = new SsdpDeviceLocator())
            {
                var devices = new List<SsdpDevice>();
                foreach (var discoveredDevice in await deviceLocator.SearchAsync(_options.SearchDuration).ConfigureAwait(false))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (Convert.ToString(discoveredDevice.DescriptionLocation)?.Contains("0.0.0.0", StringComparison.Ordinal) == true)
                    {
                        continue;
                    }

                    try
                    {
                        var ssdpDevice = await discoveredDevice.GetDeviceInfo(_httpClient).ConfigureAwait(false);
                        devices.Add(CreateDeviceModel(discoveredDevice, ssdpDevice));
                    }
                    catch (Exception exception)
                    {
                        _logger.LogDebug(exception, $"Error while loading device info from '{discoveredDevice.DescriptionLocation}.'");
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

        static SsdpDevice CreateDeviceModel(DiscoveredSsdpDevice discoveredSsdpDevice, Rssdp.SsdpDevice ssdpDevice)
        {
            return new SsdpDevice
            {
                Usn = discoveredSsdpDevice.Usn,
                DescriptionLocation = discoveredSsdpDevice.DescriptionLocation?.ToString(),
                CacheLifetime = discoveredSsdpDevice.CacheLifetime,
                DeviceType = ssdpDevice.DeviceType,
                NotificationType = discoveredSsdpDevice.NotificationType,
                DeviceTypeNamespace = ssdpDevice.DeviceTypeNamespace,
            };
        }
    }
}
