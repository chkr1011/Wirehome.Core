using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Wirehome.Core.App;
using Wirehome.Core.Components.Exceptions;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.HTTP.Controllers;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Components.Groups
{
    /// <summary>
    /// TODO: Create bus messages when something has changed (settings etc.)
    /// </summary>
    public sealed class ComponentGroupRegistryService : WirehomeCoreService
    {
        const string ComponentGroupsDirectory = "ComponentGroups";

        readonly Dictionary<string, ComponentGroup> _componentGroups = new Dictionary<string, ComponentGroup>();

        readonly StorageService _storageService;
        readonly MessageBusService _messageBusService;
        readonly AppService _appService;
        readonly ILogger _logger;

        public ComponentGroupRegistryService(
            StorageService storageService,
            SystemStatusService systemInformationService,
            MessageBusService messageBusService,
            AppService appService,
            ILogger<ComponentGroupRegistryService> logger)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _appService = appService ?? throw new ArgumentNullException(nameof(appService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (systemInformationService == null) throw new ArgumentNullException(nameof(systemInformationService));
            systemInformationService.Set("component_group_registry.count", () => _componentGroups.Count);

            appService.RegisterStatusProvider("componentGroups", () =>
            {
                return GetComponentGroups().Select(ComponentGroupsController.CreateComponentGroupModel);
            });
        }

        public List<string> GetComponentGroupUids()
        {
            return _storageService.EnumerateDirectories("*", ComponentGroupsDirectory);
        }

        public ComponentGroupConfiguration ReadComponentGroupConfiguration(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            if (!_storageService.TryReadSerializedValue(out ComponentGroupConfiguration configuration, ComponentGroupsDirectory, uid, DefaultFileNames.Configuration))
            {
                throw new ComponentGroupNotFoundException(uid);
            }

            return configuration;
        }

        public void WriteComponentGroupConfiguration(string uid, ComponentGroupConfiguration configuration)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            _storageService.WriteSerializedValue(configuration, ComponentGroupsDirectory, uid, DefaultFileNames.Configuration);
        }

        public void DeleteComponentGroup(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_componentGroups)
            {
                _storageService.DeletePath(ComponentGroupsDirectory, uid);

                _componentGroups.Remove(uid);
            }
        }

        public void InitializeComponentGroup(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            if (!_storageService.TryReadSerializedValue(out ComponentGroupConfiguration configuration, ComponentGroupsDirectory, uid, DefaultFileNames.Configuration))
            {
                throw new ComponentGroupNotFoundException(uid);
            }

            if (!_storageService.TryReadSerializedValue(out IDictionary<string, object> settings, ComponentGroupsDirectory, uid, DefaultFileNames.Settings))
            {
                settings = new Dictionary<string, object>();
            }

            var componentGroup = new ComponentGroup(uid);
            foreach (var setting in settings)
            {
                componentGroup.SetSetting(setting.Key, setting.Value);
            }

            var associationUids = _storageService.EnumerateDirectories("*", ComponentGroupsDirectory, uid, "Components");
            foreach (var associationUid in associationUids)
            {
                if (!_storageService.TryReadSerializedValue(out IDictionary<string, object> associationSettings, ComponentGroupsDirectory, uid, "Components", associationUid, DefaultFileNames.Settings))
                {
                    associationSettings = new Dictionary<string, object>();
                }

                var componentAssociation = new ComponentGroupAssociation();
                foreach (var associationSetting in associationSettings)
                {
                    componentAssociation.Settings[associationSetting.Key] = associationSetting.Value;
                }

                componentGroup.Components.TryAdd(associationUid, componentAssociation);
            }

            associationUids = _storageService.EnumerateDirectories("*", ComponentGroupsDirectory, uid, "Macros");
            foreach (var associationUid in associationUids)
            {
                if (!_storageService.TryReadSerializedValue(out IDictionary<string, object> associationSettings, ComponentGroupsDirectory, uid, "Macros", associationUid, DefaultFileNames.Settings))
                {
                    associationSettings = new Dictionary<string, object>();
                }

                var componentAssociation = new ComponentGroupAssociation();
                foreach (var associationSetting in associationSettings)
                {
                    componentAssociation.Settings[associationSetting.Key] = associationSetting.Value;
                }

                componentGroup.Macros.TryAdd(associationUid, componentAssociation);
            }

            lock (_componentGroups)
            {
                _componentGroups[uid] = componentGroup;
            }
        }

        public void TryInitializeComponentGroup(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            try
            {
                InitializeComponentGroup(uid);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while initializing component group '{uid}'.'");
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

        public List<ComponentGroup> GetComponentGroupsWithTag(string tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));

            lock (_componentGroups)
            {
                return _componentGroups.Values.Where(g => g.HasTag(tag)).ToList();
            }
        }

        public void AssignComponent(string componentGroupUid, string componentUid)
        {
            if (componentGroupUid == null) throw new ArgumentNullException(nameof(componentGroupUid));
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));

            lock (_componentGroups)
            {
                var componentGroup = GetComponentGroup(componentGroupUid);

                if (!componentGroup.Components.TryAdd(componentUid, new ComponentGroupAssociation()))
                {
                    return;
                }

                Save(componentGroup);
            }
        }

        public void UnassignComponent(string componentGroupUid, string componentUid)
        {
            if (componentGroupUid == null) throw new ArgumentNullException(nameof(componentGroupUid));
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));

            lock (_componentGroups)
            {
                var componentGroup = GetComponentGroup(componentGroupUid);

                if (!componentGroup.Components.Remove(componentUid, out _))
                {
                    return;
                }

                Save(componentGroup);
            }
        }

        public bool SetComponentGroupTag(string componentGroupUid, string tag)
        {
            if (componentGroupUid == null) throw new ArgumentNullException(nameof(componentGroupUid));

            var componentGroup = GetComponentGroup(componentGroupUid);

            if (!componentGroup.SetTag(tag))
            {
                return false;
            }

            _storageService.WriteSerializedValue(componentGroup.GetTags(), ComponentGroupsDirectory, componentGroup.Uid, DefaultFileNames.Tags);

            //_messageBusWrapper.PublishTagAddedEvent(componentGroup.Uid, tag);

            _logger.LogDebug("Component group '{0}' tag '{1}' set.", componentGroup.Uid, tag);

            return true;
        }

        public bool RemoveComponentGroupTag(string componentGroupUid, string tag)
        {
            if (componentGroupUid == null) throw new ArgumentNullException(nameof(componentGroupUid));

            var componentGroup = GetComponentGroup(componentGroupUid);

            if (!componentGroup.RemoveTag(tag))
            {
                return false;
            }

            _storageService.WriteSerializedValue(componentGroup.GetTags(), ComponentGroupsDirectory, componentGroup.Uid, DefaultFileNames.Tags);

            //_messageBusWrapper.PublishTagRemovedEvent(componentGroup.Uid, tag);

            _logger.LogDebug("Component group '{0}' tag '{1}' removed.", componentGroup.Uid, tag);

            return true;
        }

        public void AssignMacro(string componentGroupUid, string macroUid)
        {
            if (componentGroupUid == null) throw new ArgumentNullException(nameof(componentGroupUid));
            if (macroUid == null) throw new ArgumentNullException(nameof(macroUid));

            lock (_componentGroups)
            {
                var componentGroup = GetComponentGroup(componentGroupUid);

                if (!componentGroup.Macros.TryAdd(macroUid, new ComponentGroupAssociation()))
                {
                    return;
                }

                Save(componentGroup);
            }
        }

        public void UnassignMacro(string componentGroupUid, string macroUid)
        {
            if (componentGroupUid == null) throw new ArgumentNullException(nameof(componentGroupUid));
            if (macroUid == null) throw new ArgumentNullException(nameof(macroUid));

            lock (_componentGroups)
            {
                var componentGroup = GetComponentGroup(componentGroupUid);

                if (!componentGroup.Macros.Remove(macroUid, out _))
                {
                    return;
                }

                Save(componentGroup);
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

                SaveComponentAssociationSettings(componentGroup, componentUid, association);
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

                Save(componentGroup);
            }
        }

        public object GetComponentGroupSetting(string componentGroupUid, string settingUid)
        {
            if (componentGroupUid == null) throw new ArgumentNullException(nameof(componentGroupUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            ComponentGroup componentGroup;
            lock (_componentGroups)
            {
                if (!_componentGroups.TryGetValue(componentGroupUid, out componentGroup))
                {
                    return null;
                }
            }

            componentGroup.TryGetSetting(settingUid, out var value);
            return value;
        }

        public void SetComponentGroupSetting(string componentGroupUid, string settingUid, object value)
        {
            if (componentGroupUid == null) throw new ArgumentNullException(nameof(componentGroupUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            var componentGroup = GetComponentGroup(componentGroupUid);

            var setSettingResult = componentGroup.SetSetting(settingUid, value);
            if (!setSettingResult.IsNewValue)
            {
                if (Equals(setSettingResult.OldValue, value))
                {
                    return;
                }
            }

            _storageService.WriteSerializedValue(componentGroup.GetSettings(), ComponentGroupsDirectory, componentGroupUid, DefaultFileNames.Settings);

            _logger.Log(
                LogLevel.Debug,
                "Component group '{0}' setting '{1}' changed from '{2}' to '{3}'.",
                componentGroupUid,
                settingUid,
                setSettingResult.OldValue ?? "<null>",
                value ?? "<null>");

            _messageBusService.Publish(new Dictionary<object, object>
            {
                ["type"] = "component_group_registry.event.setting_changed",
                ["component_group_uid"] = componentGroupUid,
                ["setting_uid"] = settingUid,
                ["old_value"] = setSettingResult.OldValue,
                ["new_value"] = value
            });
        }

        public object RemoveComponentGroupSetting(string componentGroupUid, string settingUid)
        {
            if (componentGroupUid == null) throw new ArgumentNullException(nameof(componentGroupUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            var componentGroup = GetComponentGroup(componentGroupUid);

            if (!componentGroup.RemoveSetting(settingUid, out var oldValue))
            {
                return null;
            }

            _storageService.WriteSerializedValue(componentGroup.GetSettings(), ComponentGroupsDirectory, componentGroupUid, DefaultFileNames.Settings);

            _logger.Log(
                LogLevel.Debug,
                "Component group '{0}' setting '{1}' removed.",
                componentGroupUid,
                settingUid,
                oldValue ?? "<null>");

            _messageBusService.Publish(new Dictionary<object, object>
            {
                ["type"] = "component_group_registry.event.setting_removed",
                ["component_group_uid"] = componentGroupUid,
                ["setting_uid"] = settingUid,
                ["old_value"] = oldValue
            });

            return oldValue;
        }

        protected override void OnStart()
        {
            foreach (var componentGroupUid in GetComponentGroupUids())
            {
                TryInitializeComponentGroup(componentGroupUid);
            }
        }

        void Save(ComponentGroup componentGroup)
        {
            var configuration = new ComponentGroupConfiguration();
            _storageService.WriteSerializedValue(configuration, ComponentGroupsDirectory, componentGroup.Uid, DefaultFileNames.Configuration);

            foreach (var componentAssociation in componentGroup.Components)
            {
                SaveComponentAssociationSettings(componentGroup, componentAssociation.Key, componentAssociation.Value);
            }

            foreach (var componentAssociation in componentGroup.Macros)
            {
                SaveMacroAssociationSettings(componentGroup, componentAssociation.Key, componentAssociation.Value);
            }
        }

        void SaveComponentAssociationSettings(ComponentGroup componentGroup, string componentUid, ComponentGroupAssociation association)
        {
            _storageService.WriteSerializedValue(association.Settings, ComponentGroupsDirectory, componentGroup.Uid, "Components", componentUid, DefaultFileNames.Settings);
        }

        void SaveMacroAssociationSettings(ComponentGroup componentGroup, string componentUid, ComponentGroupAssociation association)
        {
            _storageService.WriteSerializedValue(association.Settings, ComponentGroupsDirectory, componentGroup.Uid, "Macros", componentUid, DefaultFileNames.Settings);
        }
    }
}
