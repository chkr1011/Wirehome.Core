using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components.Configuration;
using Wirehome.Core.Components.Exceptions;
using Wirehome.Core.Constants;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Extensions;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Model;
using Wirehome.Core.Python.Models;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Components
{
    public class ComponentRegistryService : IService
    {
        private const string ComponentsDirectory = "Components";

        private readonly Dictionary<string, Component> _components = new Dictionary<string, Component>();

        private readonly ComponentRegistryMessageBusProxy _messageBusProxy;
        private readonly StorageService _storageService;
        private readonly MessageBusService _messageBusService;
        private readonly ComponentInitializerService _componentInitializerService;
        private readonly ILogger _logger;

        public ComponentRegistryService(
            StorageService storageService,
            SystemStatusService systemStatusService,
            MessageBusService messageBusService,
            ComponentInitializerService componentInitializerService,
            ILogger<ComponentRegistryService> logger)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _componentInitializerService = componentInitializerService ?? throw new ArgumentNullException(nameof(componentInitializerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _messageBusProxy = new ComponentRegistryMessageBusProxy(messageBusService);

            if (systemStatusService == null) throw new ArgumentNullException(nameof(systemStatusService));
            systemStatusService.Set("component_registry.count", () => _components.Count);
        }

        public void Start()
        {
            foreach (var componentUid in GetComponentUids())
            {
                TryInitializeComponent(componentUid);
            }

            AttachToMessageBus();
        }

        public void WriteComponentConfiguration(string uid, ComponentConfiguration configuration)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            _storageService.Write(configuration, ComponentsDirectory, uid, DefaultFilenames.Configuration);
        }

        public ComponentConfiguration ReadComponentConfiguration(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            if (!_storageService.TryRead(out ComponentConfiguration configuration, ComponentsDirectory, uid, DefaultFilenames.Configuration))
            {
                throw new ComponentNotFoundException(uid);
            }

            return configuration;
        }

        public void DeleteComponent(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _storageService.DeleteDirectory(ComponentsDirectory, uid);
        }

        public void TryInitializeComponent(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            
            try
            {
                if (!_storageService.TryRead(out ComponentConfiguration configuration, ComponentsDirectory, uid, DefaultFilenames.Configuration))
                {
                    throw new ComponentNotFoundException(uid);
                }

                if (!configuration.IsEnabled)
                {
                    _logger.LogInformation($"Component '{uid}' not initialized because it is disabled.");
                    return;
                }

                if (!_storageService.TryRead(out WirehomeDictionary settings, ComponentsDirectory, uid, DefaultFilenames.Settings))
                {
                    settings = new WirehomeDictionary();
                }
                
                var component = new Component(uid);
                foreach (var setting in settings)
                {
                    component.Settings[setting.Key] = setting.Value;
                }

                lock (_components)
                {
                    if (_components.TryGetValue(uid, out var existingComponent))
                    {
                        existingComponent.ProcessMessage(new WirehomeDictionary().WithType(ControlType.Destroy));
                    }

                    _components[uid] = component;
                }

                _componentInitializerService.Create(this).InitializeComponent(component, configuration);
                component.ProcessMessage(new WirehomeDictionary().WithType(ControlType.Initialize));

                _logger.LogInformation($"Component '{component.Uid}' initialized successfully.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while initializing component '{uid}'.");

                lock (_components)
                {
                    _components.Remove(uid, out _);
                }
            }
        }

        public Component GetComponent(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_components)
            {
                if (!_components.TryGetValue(uid, out var component))
                {
                    throw new ComponentNotFoundException(uid);
                }

                return component;
            }
        }

        public List<Component> GetComponents()
        {
            lock (_components)
            {
                return new List<Component>(_components.Values);
            }
        }

        public bool TryGetComponent(string uid, out Component component)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_components)
            {
                return _components.TryGetValue(uid, out component);
            }
        }

        public void SetComponentConfiguration(string componentUid, string configurationUid, object value)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (configurationUid == null) throw new ArgumentNullException(nameof(configurationUid));

            GetComponent(componentUid)?.Configuration?.SetValue(configurationUid, value);
        }

        public object GetComponentConfiguration(string componentUid, string configurationUid, object defaultValue = null)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (configurationUid == null) throw new ArgumentNullException(nameof(configurationUid));

            var component = GetComponent(componentUid);
            return component.Configuration.GetValueOrDefault(configurationUid, defaultValue);
        }

        public bool ComponentHasStatus(string componentUid, string statusUid)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (statusUid == null) throw new ArgumentNullException(nameof(statusUid));

            var component = GetComponent(componentUid);
            return component.Status.ContainsKey(statusUid);
        }

        public object GetComponentStatus(string componentUid, string statusUid, object defaultValue = null)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (statusUid == null) throw new ArgumentNullException(nameof(statusUid));

            var component = GetComponent(componentUid);
            return component.Status.GetValueOrDefault(statusUid, defaultValue);
        }

        public void SetComponentStatus(string componentUid, string statusUid, object value)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (statusUid == null) throw new ArgumentNullException(nameof(statusUid));

            var component = GetComponent(componentUid);

            var isAdd = true;
            object oldValue = null;

            component.Status.AddOrUpdate(statusUid, value, (_, v) =>
            {
                isAdd = false;
                oldValue = v;
                return value;
            });

            var oldValueString = Convert.ToString(oldValue, CultureInfo.InvariantCulture);
            var newValueString = Convert.ToString(value, CultureInfo.InvariantCulture);
            var hasChanged = isAdd || !string.Equals(oldValueString, newValueString, StringComparison.Ordinal);

            if (!hasChanged)
            {
                return;
            }

            _messageBusProxy.PublishStatusChangedBusMessage(component.Uid, statusUid, oldValue, value);

            _logger.LogDebug(
                "Component '{0}' changed '{1}' ({2} -> {3}).",
                component.Uid,
                statusUid,
                oldValueString,
                newValueString);
        }

        public bool ComponentHasSetting(string componentUid, string settingUid)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            var component = GetComponent(componentUid);
            return component.Settings.ContainsKey(settingUid);
        }

        public object GetComponentSetting(string componentUid, string settingUid, object defaultValue = null)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            var component = GetComponent(componentUid);
            return component.Settings.GetValueOrDefault(settingUid, defaultValue);
        }

        public void RegisterComponentSetting(string componentUid, string settingUid, object value)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            var component = GetComponent(componentUid);
            if (component.Settings.TryGetValue(settingUid, out _))
            {
                return;
            }

            SetComponentSetting(componentUid, settingUid, value);
        }

        public void SetComponentSetting(string componentUid, string settingUid, object value)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            var component = GetComponent(componentUid);
            component.Settings.TryGetValue(settingUid, out var oldValue);

            if (Equals(oldValue, value))
            {
                return;
            }

            component.Settings[settingUid] = value;

            _storageService.Write(component.Settings, "Components", component.Uid, "Settings.json");
            _messageBusProxy.PublishSettingChangedBusMessage(component.Uid, settingUid, oldValue, value);
        }

        public object RemoveComponentSetting(string componentUid, string settingUid)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            var component = GetComponent(componentUid);
            component.Settings.Remove(settingUid, out var value);

            _storageService.Write(component.Settings, "Components", component.Uid, "Settings.json");
            _messageBusProxy.PublishSettingRemovedBusMessage(component.Uid, settingUid, value);

            return value;
        }

        public WirehomeDictionary ProcessComponentMessage(string componentUid, WirehomeDictionary message)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (message == null) throw new ArgumentNullException(nameof(message));

            Component component = GetComponent(componentUid);

            try
            {
                return component.ProcessMessage(message);
            }
            catch (Exception exception)
            {
                return new ExceptionPythonModel(exception).ConvertToPythonDictionary();
            }
        }

        public List<string> GetComponentUids()
        {
            return _storageService.EnumeratureDirectories("*", ComponentsDirectory);
        }

        private void AttachToMessageBus()
        {
            var filter = new WirehomeDictionary().WithType("component_registry.process_message");
            _messageBusService.Subscribe("component_registry.process_message", filter, OnBusMessageExecuteCommand);
        }

        private void OnBusMessageExecuteCommand(MessageBusMessage busMessage)
        {
            var message = busMessage.Message;
            var componentUid = Convert.ToString(message["component_uid"], CultureInfo.InvariantCulture);

            // TODO: Refactor this conversion!
            var innerMessage = (WirehomeDictionary)message["message"];

            _messageBusService.PublishResponse(message, ProcessComponentMessage(componentUid, innerMessage));
        }
    }
}
