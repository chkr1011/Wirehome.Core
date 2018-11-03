using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components.Configuration;
using Wirehome.Core.Components.Exceptions;
using Wirehome.Core.Constants;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Extensions;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Model;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Models;
using Wirehome.Core.Python.Proxies;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Components
{
    public class ComponentRegistryService
    {
        private readonly Dictionary<string, Component> _components = new Dictionary<string, Component>();

        private readonly ComponentRegistryMessageBusProxy _messageBusProxy;
        private readonly StorageService _storageService;
        private readonly MessageBusService _messageBusService;
        private readonly ComponentInitializerFactory _componentInitializerFactory;
        private readonly ILogger _logger;

        public ComponentRegistryService(
            StorageService storageService,
            SystemStatusService systemStatusService,
            MessageBusService messageBusService,
            ComponentInitializerFactory componentInitializerFactory,
            PythonEngineService pythonEngineService,
            ILoggerFactory loggerFactory)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _componentInitializerFactory = componentInitializerFactory ?? throw new ArgumentNullException(nameof(componentInitializerFactory));

            _messageBusProxy = new ComponentRegistryMessageBusProxy(messageBusService);

            pythonEngineService.RegisterSingletonProxy(new ComponentRegistryPythonProxy(this));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<ComponentRegistryService>();

            if (systemStatusService == null) throw new ArgumentNullException(nameof(systemStatusService));
            systemStatusService.Set("component_registry.count", () => _components.Count);
        }

        public void Start()
        {
            Load();
            AttachToMessageBus();
        }

        public bool TryInitializeComponent(string uid, ComponentConfiguration configuration, out Component component)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            try
            {
                component = new Component(uid);
                lock (_components)
                {
                    if (_components.TryGetValue(uid, out var existingComponent))
                    {
                        // TODO: Convert this to a real method call.
                        existingComponent.ProcessMessage(new WirehomeDictionary().WithType("destroy"));
                    }

                    _components[uid] = component;
                }

                _componentInitializerFactory.Create(this).InitializeComponent(component, configuration);

                _logger.LogInformation($"Component '{component.Uid}' initialized successfully.");

                return true;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while initializing component '{uid}'.");

                lock (_components)
                {
                    _components.Remove(uid, out _);
                }

                component = null;
                return false;
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

            component.Status.AddOrUpdate(statusUid, value, (s, v) =>
            {
                isAdd = false;
                oldValue = v;
                return value;
            });

            var oldValueString = Convert.ToString(oldValue, CultureInfo.InvariantCulture);
            var newValueString = Convert.ToString(value, CultureInfo.InvariantCulture);
            var hasChanged = isAdd || !string.Equals(oldValueString, newValueString, StringComparison.Ordinal);

            if (hasChanged)
            {
                _messageBusProxy.PublishStatusChangedBusMessage(component.Uid, statusUid, oldValue, value);

                _logger.LogDebug(
                    "Status '{0}' of component '{1}' changed from '{2}' to '{3}'.",
                    statusUid,
                    component.Uid,
                    oldValueString,
                    newValueString);
            }
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

            Component component;
            lock (_components)
            {
                if (!_components.TryGetValue(componentUid, out component))
                {
                    return new WirehomeDictionary()
                        .WithValue("type", ControlType.ParameterInvalidException)
                        .WithValue("parameter_name", nameof(componentUid))
                        .WithValue("parameter_value", componentUid);
                }
            }

            try
            {
                return component.ProcessMessage(message);
            }
            catch (Exception exception)
            {
                return new ExceptionPythonModel(exception).ConvertToPythonDictionary();
            }
        }

        private void Load()
        {
            lock (_components)
            {
                _components.Clear();
            }

            var configurationFiles = _storageService.EnumerateFiles("Configuration.json", "Components");
            foreach (var configurationFile in configurationFiles)
            {
                if (_storageService.TryRead(out ComponentConfiguration configuration, "Components", configurationFile))
                {
                    var componentUid = Path.GetDirectoryName(configurationFile);

                    if (configuration.IsEnabled)
                    {
                        TryInitializeComponent(componentUid, configuration, out _);
                    }
                }
            }
        }

        private void AttachToMessageBus()
        {
            var filter = new WirehomeDictionary().WithType("component_registry.process_message");
            _messageBusService.Subscribe("component_registry.execute_command", filter, OnBusMessageExecuteCommand);
        }

        private void OnBusMessageExecuteCommand(MessageBusMessage busMessage)
        {
            var message = busMessage.Message;
            var componentUid = Convert.ToString(message["component_uid"], CultureInfo.InvariantCulture);

            // TODO: Refactor this conversion!
            var innerMessage = (WirehomeDictionary)message["message"];

            _messageBusService.PublishResponse(busMessage, ProcessComponentMessage(componentUid, innerMessage));
        }
    }
}
