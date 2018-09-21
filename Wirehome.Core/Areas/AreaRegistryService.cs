using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Areas.Exceptions;
using Wirehome.Core.Components;
using Wirehome.Core.Constants;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Model;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Areas
{
    public class AreaRegistryService
    {
        private readonly ConcurrentDictionary<string, Area> _areas = new ConcurrentDictionary<string, Area>();

        private readonly StorageService _storageService;
        private readonly ComponentRegistryService _componentRegistryService;
        private readonly MessageBusService _messageBusService;

        private readonly ILogger _logger;

        public AreaRegistryService(
            StorageService storageService,
            SystemStatusService systemInformationService, 
            ComponentRegistryService componentRegistryService,
            MessageBusService messageBusService,
            ILoggerFactory loggerFactory)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<AreaRegistryService>();

            if (systemInformationService == null) throw new ArgumentNullException(nameof(systemInformationService));
            systemInformationService.Set("area_registry.areas_count", () => _areas.Count);
        }

        public void Start()
        {
            lock (_areas)
            {
                Load();
            }
        }

        public List<Area> GetAreas()
        {
            lock (_areas)
            {
                return new List<Area>(_areas.Values);
            }
        }

        public Area GetArea(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            if (!_areas.TryGetValue(uid, out var area))
            {
                throw new AreaNotFoundException(uid);
            }

            return area;
        }

        public bool TryGetArea(string uid, out Area area)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            return _areas.TryGetValue(uid, out area);
        }

        public void UpdateArea(string uid, AreaConfiguration areaConfiguration)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (areaConfiguration == null) throw new ArgumentNullException(nameof(areaConfiguration));

            _areas[uid] = InitializeArea(uid, areaConfiguration);

            Save();
            
            _logger.Log(LogLevel.Information, $"Updated area '{uid}'.");
        }

        public List<Component> GetComponentsOfArea(string areaUid)
        {
            var area = GetArea(areaUid);
            var components = new List<Component>();

            foreach (var componentUid in area.Components)
            {
                if (_componentRegistryService.TryGetComponent(componentUid, out var component))
                {
                    components.Add(component);
                }
            }

            return components;
        }

        public WirehomeDictionary ExecuteComponentCommand(string areaUid, WirehomeDictionary parameters)
        {
            if (areaUid == null) throw new ArgumentNullException(nameof(areaUid));

            Area area;
            lock (_areas)
            {
                if (!_areas.TryGetValue(areaUid, out area))
                {
                    return null;
                }
            }

            var result = new WirehomeDictionary().WithType(ControlType.Initialize);

            foreach (var assignedComponent in area.Components)
            {
                result[assignedComponent] = _componentRegistryService.ExecuteComponentCommand(assignedComponent, parameters);
            }

            return result;
        }

        public object GetAreaSetting(string areaUid, string settingUid)
        {
            if (areaUid == null) throw new ArgumentNullException(nameof(areaUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            if (!_areas.TryGetValue(areaUid, out var area))
            {
                return null;
            }

            return area.Settings.GetValueOrDefault(settingUid);
        }

        public void SetAreaSetting(string areaUid, string settingUid, object value)
        {
            if (areaUid == null) throw new ArgumentNullException(nameof(areaUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            if (!_areas.TryGetValue(areaUid, out var area))
            {
                return;
            }

            area.Settings.TryGetValue(settingUid, out var oldValue);
            if (Equals(oldValue, value))
            {
                return;
            }

            area.Settings[settingUid] = value;
            _storageService.Write(area.Settings, "Areas", areaUid);
            
            _logger.Log(
                LogLevel.Debug,
                "Area '{0}' setting '{1}' changed from '{2}' to '{3}'.",
                areaUid,
                settingUid,
                oldValue ?? "<null>",
                value ?? "<null>");
            
            _messageBusService.Publish(new WirehomeDictionary()
                .WithType("area_registry.event.setting_changed")
                .WithValue("area_uid", areaUid)
                .WithValue("setting_uid", settingUid)
                .WithValue("old_value", oldValue)
                .WithValue("new_value", oldValue)
                .WithValue("timestamp", DateTime.Now));
        }

        private void Load()
        {
            _areas.Clear();

            var configurationFiles = _storageService.EnumerateFiles("Configuration.json", "Areas");
            foreach (var configurationFile in configurationFiles)
            {
                if (_storageService.TryRead(out AreaConfiguration configuration, "Areas", configurationFile))
                {
                    var uid = Path.GetDirectoryName(configurationFile);

                    var area = InitializeArea(uid, configuration);
                    _areas[area.Uid] = area;
                }
            }
        }

        private void Save()
        {
            var configurations = new Dictionary<string, AreaConfiguration>();

            foreach (var area in _areas.Values)
            {
                var configuration = new AreaConfiguration();
                
                foreach (var component in area.Components)
                {
                    configuration.Components.Add(component);
                }
            }

            _storageService.Write(configurations, "Areas.json");
        }

        private Area InitializeArea(string uid, AreaConfiguration configuration)
        {
            var area = new Area(uid);

            if (_storageService.TryRead(out WirehomeDictionary settings, "Areas", uid, "Settings.json"))
            {
                foreach (var setting in settings)
                {
                    area.Settings[setting.Key] = setting.Value;
                }
            }
            
            if (configuration.Components != null)
            {
                foreach (var assignedComponent in configuration.Components)
                {
                    area.Components.Add(assignedComponent);
                }
            }

            return area;
        }
    }
}
