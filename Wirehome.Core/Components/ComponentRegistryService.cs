using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components.Configuration;
using Wirehome.Core.Constants;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Extensions;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Model;
using Wirehome.Core.Python;
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
            systemStatusService.Set("component_registry.components_count", () => _components.Count);
        }

        public void Start()
        {
            Load();
            AttachToMessageBus();
        }

        public bool TryInitializeComponent(string uid, ComponentConfiguration configuration, out Component component)
        {
            try
            {
                component = new Component(uid);
                lock (_components)
                {
                    _components[uid] = component;
                }

                _componentInitializerFactory.Create(this).InitializeComponent(component, configuration);

                _logger.Log(LogLevel.Information, $"Component '{component.Uid}' initialized successfully.");

                return true;
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, exception, $"Error while initializing component '{uid}'.");

                lock (_components)
                {
                    _components.Remove(uid, out _);
                }
            }

            component = null;
            return false;
        }

        public Component GetComponent(string uid)
        {
            lock (_components)
            {
                if (!_components.TryGetValue(uid, out var component))
                {
                    return null;
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

        public object GetComponentConfiguration(string componentUid, string configurationUid)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (configurationUid == null) throw new ArgumentNullException(nameof(configurationUid));

            return GetComponent(componentUid)?.Configuration?.GetValueOrDefault(configurationUid);
        }

        public object GetComponentStatus(string componentUid, string statusUid)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (statusUid == null) throw new ArgumentNullException(nameof(statusUid));

            return GetComponent(componentUid)?.Status?.GetValueOrDefault(statusUid);
        }

        public void SetComponentStatus(string componentUid, string statusUid, object value)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (statusUid == null) throw new ArgumentNullException(nameof(statusUid));

            var component = GetComponent(componentUid);
            if (component == null)
            {
                return;
            }

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

            _messageBusProxy.PublishStatusReportedBusMessage(component.Uid, statusUid, oldValue, value, hasChanged);

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
            else
            {
                _logger.LogDebug(
                    "Status '{0}' of component '{1}' reported to be still '{2}'.",
                    statusUid,
                    component.Uid,
                    newValueString);
            }
        }

        public object GetComponentSetting(string componentUid, string settingUid)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));
            
            Component component;
            lock (_components)
            {
                if (!_components.TryGetValue(componentUid, out component))
                {
                    return null;
                }
            }

            return component.Settings.GetValueOrDefault(settingUid);
        }

        public void SetComponentSetting(string componentUid, string settingUid, object value)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            Component component;
            lock (_components)
            {
                if (!_components.TryGetValue(componentUid, out component))
                {
                    return;
                }
            }

            component.Settings.TryGetValue(settingUid, out var oldValue);
            if (Equals(oldValue, value))
            {
                return;
            }

            component.Settings[settingUid] = value;
            _storageService.Write(component.Settings, "Components", component.Uid, "Settings.json");
            
            _logger.Log(
                LogLevel.Debug,
                "Component '{0}' setting '{1}' changed from '{2}' to '{3}'.",
                component.Uid,
                settingUid,
                oldValue ?? "<null>",
                value ?? "<null>");

            _messageBusProxy.PublishSettingChangedBusMessage(component.Uid, settingUid, oldValue, value);
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
                        .WithType(ControlType.ParameterInvalidException)
                        .WithValue("parameter_name", nameof(componentUid))
                        .WithValue("parameter_value", componentUid);
                }
            }

            return component.ProcessMessage(message);
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
            var filter = new WirehomeDictionary
            {
                ["type"] = "component_registry.execute_command"
            };

            _messageBusService.Subscribe(filter, OnBusMessageExecuteCommand);
        }

        private void OnBusMessageExecuteCommand(IDictionary message)
        {
            var componentUid = (string)message["component_uid"];
            var parameters = (WirehomeDictionary)message["parameters"];

            ProcessComponentMessage(componentUid, parameters);
        }
    }
}
