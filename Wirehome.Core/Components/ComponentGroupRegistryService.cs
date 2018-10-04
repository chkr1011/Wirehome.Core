using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components.Exceptions;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Model;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Components
{
    /// <summary>
    /// TODO: Create bus messages when something has changed (settings etc.)
    /// </summary>
    public class ComponentGroupRegistryService
    {
        private readonly Dictionary<string, ComponentGroup> _componentGroups = new Dictionary<string, ComponentGroup>();

        private readonly StorageService _storageService;
        private readonly ComponentRegistryService _componentRegistryService;
        private readonly MessageBusService _messageBusService;

        private readonly ILogger _logger;

        public ComponentGroupRegistryService(
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
            _logger = loggerFactory.CreateLogger<ComponentGroupRegistryService>();

            if (systemInformationService == null) throw new ArgumentNullException(nameof(systemInformationService));
            systemInformationService.Set("component_group_registry.count", () => _componentGroups.Count);
        }

        public void Start()
        {
            lock (_componentGroups)
            {
                Load();
            }
        }

        public List<ComponentGroup> GetComponentGroups()
        {
            lock (_componentGroups)
            {
                return new List<ComponentGroup>(_componentGroups.Values);
            }
        }

        public ComponentGroup GetComponentGroup(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_componentGroups)
            {
                if (!_componentGroups.TryGetValue(uid, out var componentGroup))
                {
                    throw new ComponentGroupNotFoundException(uid);
                }

                return componentGroup;
            }
        }

        public void CreateComponentGroup(string uid, ComponentGroupConfiguration configuration)
        {
            lock (_componentGroups)
            {
                if (!_componentGroups.TryGetValue(uid, out var componentGroup))
                {
                    componentGroup = new ComponentGroup(uid);
                }

                // Copy configuration values as soon as some are available.

                _componentGroups[uid] = componentGroup;

                Save();
            }
        }

        public void AssignComponent(string componentGroupUid, string componentUid)
        {
            if (componentGroupUid == null) throw new ArgumentNullException(nameof(componentGroupUid));
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));

            lock (_componentGroups)
            {
                if (!_componentGroups.TryGetValue(componentGroupUid, out var componentGroup))
                {
                    throw new ComponentGroupNotFoundException(componentUid);
                }

                if (componentGroup.Components.ContainsKey(componentUid))
                {
                    return;
                }

                componentGroup.Components[componentUid] = new ComponentGroupAssociation();

                Save();
            }
        }

        public void UnassignComponent(string componentGroupUid, string componentUid)
        {
            if (componentGroupUid == null) throw new ArgumentNullException(nameof(componentGroupUid));
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));

            lock (_componentGroups)
            {
                if (!_componentGroups.TryGetValue(componentGroupUid, out var componentGroup))
                {
                    throw new ComponentGroupNotFoundException(componentUid);
                }

                if (!componentGroup.Components.Remove(componentUid, out _))
                {
                    return;
                }

                Save();
            }
        }

        public object GetComponentAssociationSetting(string componentGroupUid, string componentUid, string settingUid)
        {
            if (componentGroupUid == null) throw new ArgumentNullException(nameof(componentGroupUid));
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            lock (_componentGroups)
            {
                if (!_componentGroups.TryGetValue(componentGroupUid, out var componentGroup))
                {
                    throw new ComponentGroupNotFoundException(componentUid);
                }

                if (!componentGroup.Components.TryGetValue(componentUid, out var association))
                {
                    return null;
                }

                if (!association.Settings.TryGetValue(settingUid, out var settingValue))
                {
                    return null;
                }

                return settingValue;
            }
        }

        public void SetComponentAssociationSetting(string componentGroupUid, string componentUid, string settingUid, object settingValue)
        {
            if (componentGroupUid == null) throw new ArgumentNullException(nameof(componentGroupUid));
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            lock (_componentGroups)
            {
                if (!_componentGroups.TryGetValue(componentGroupUid, out var componentGroup))
                {
                    throw new ComponentGroupNotFoundException(componentUid);
                }

                if (!componentGroup.Components.TryGetValue(componentUid, out var association))
                {
                    return;
                }

                association.Settings[settingUid] = settingValue;

                Save();
            }
        }

        public void RemoveComponentAssociationSetting(string componentGroupUid, string componentUid, string settingUid)
        {
            if (componentGroupUid == null) throw new ArgumentNullException(nameof(componentGroupUid));
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            lock (_componentGroups)
            {
                if (!_componentGroups.TryGetValue(componentGroupUid, out var componentGroup))
                {
                    throw new ComponentGroupNotFoundException(componentUid);
                }

                if (!componentGroup.Components.TryGetValue(componentUid, out var association))
                {
                    return;
                }

                if (!association.Settings.Remove(settingUid, out _))
                {
                    return;
                }

                Save();
            }
        }

        //public bool TryGetComponentGroup(string uid, out ComponentGroup componentGroup)
        //{
        //    if (uid == null) throw new ArgumentNullException(nameof(uid));

        //    return _componentGroups.TryGetValue(uid, out componentGroup);
        //}

        ////public void InitializeComponentGroup(string uid, ComponentGroupConfigurationOld configuration)
        ////{
        ////    if (uid == null) throw new ArgumentNullException(nameof(uid));
        ////    if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        ////    _componentGroups[uid] = InitializeComponentGroupInternal(uid, configuration);

        ////    Save();

        ////    _logger.Log(LogLevel.Information, $"Initialized component group '{uid}'.");
        ////}

        ////public List<Component> GetAssignedComponents(string uid)
        ////{
        ////    if (uid == null) throw new ArgumentNullException(nameof(uid));

        ////    var componentGroup = GetComponentGroup(uid);
        ////    var components = new List<Component>();

        ////    foreach (var componentUid in componentGroup.Components)
        ////    {
        ////        if (_componentRegistryService.TryGetComponent(componentUid, out var component))
        ////        {
        ////            components.Add(component);
        ////        }
        ////    }

        ////    return components;
        ////}

        public object GetComponentGroupSetting(string uid, string settingUid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            if (!_componentGroups.TryGetValue(uid, out var componentGroup))
            {
                return null;
            }

            return componentGroup.Settings.GetValueOrDefault(settingUid);
        }

        public void SetComponentGroupSetting(string uid, string settingUid, object value)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            if (!_componentGroups.TryGetValue(uid, out var componentGroup))
            {
                return;
            }

            componentGroup.Settings.TryGetValue(settingUid, out var oldValue);
            if (Equals(oldValue, value))
            {
                return;
            }

            componentGroup.Settings[settingUid] = value;
            _storageService.Write(componentGroup.Settings, "ComponentGroups", uid);

            _logger.Log(
                LogLevel.Debug,
                "Component group '{0}' setting '{1}' changed from '{2}' to '{3}'.",
                uid,
                settingUid,
                oldValue ?? "<null>",
                value ?? "<null>");

            _messageBusService.Publish(new WirehomeDictionary()
                .WithType("component_group_registry.event.setting_changed")
                .WithValue("component_group_uid", uid)
                .WithValue("setting_uid", settingUid)
                .WithValue("old_value", oldValue)
                .WithValue("new_value", oldValue));
        }

        private void Save()
        {
            foreach (var componentGroup in _componentGroups.Values)
            {
                var configuration = new ComponentGroupConfiguration();
                _storageService.Write(configuration, "ComponentGroups", componentGroup.Uid, "Configuration.json");

                foreach (var componentAssociation in componentGroup.Components)
                {
                    _storageService.Write(componentAssociation.Value.Settings, "ComponentGroups", componentGroup.Uid, "Components", componentAssociation.Key, "Settings.json");
                }

                foreach (var componentAssociation in componentGroup.Macros)
                {
                    _storageService.Write(componentAssociation.Value.Settings, "ComponentGroups", componentGroup.Uid, "Macros", componentAssociation.Key, "Settings.json");
                }
            }
        }

        private void Load()
        {
            _componentGroups.Clear();

            var configurationFiles = _storageService.EnumerateFiles("Configuration.json", "ComponentGroups");
            foreach (var configurationFile in configurationFiles)
            {
                if (_storageService.TryRead(out ComponentGroupConfiguration _, "ComponentGroups", configurationFile))
                {
                    var componentGroupUid = Path.GetDirectoryName(configurationFile);
                    var componentGroup = new ComponentGroup(componentGroupUid);

                    var componentSettingsFiles = _storageService.EnumerateFiles("Settings.json", "ComponentGroups", componentGroupUid, "Components");
                    foreach (var componentSettingsFile in componentSettingsFiles)
                    {
                        if (_storageService.TryRead(out WirehomeDictionary settings, "ComponentGroups", componentGroupUid, "Components", componentSettingsFile))
                        {
                            var componentUid = Path.GetDirectoryName(componentSettingsFile);
                            var association = new ComponentGroupAssociation
                            {
                                Settings = settings ?? new WirehomeDictionary()
                            };

                            componentGroup.Components[componentUid] = association;
                        }
                    }

                    _componentGroups[componentGroup.Uid] = componentGroup;
                }
            }
        }

        public object RemoveComponentGroupSetting(string componentGroupUid, string settingUid)
        {
            throw new NotImplementedException();
        }
    }
}
