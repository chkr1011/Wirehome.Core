using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Automations.Configuration;
using Wirehome.Core.Automations.Exceptions;
using Wirehome.Core.Contracts;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Model;
using Wirehome.Core.Python;
using Wirehome.Core.Repository;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Automations
{
    public class AutomationRegistryService : IService
    {
        private const string AutomationsDirectory = "Automations";

        private readonly Dictionary<string, AutomationInstance> _automations = new Dictionary<string, AutomationInstance>();

        private readonly PackageManagerService _packageManagerService;
        private readonly PythonScriptHostFactoryService _pythonScriptHostFactoryService;
        private readonly StorageService _storageService;
        private readonly MessageBusService _messageBusService;
        private readonly ILogger _logger;

        public AutomationRegistryService(
            PackageManagerService packageManagerService,
            PythonScriptHostFactoryService pythonScriptHostFactoryService,
            StorageService storageService,
            MessageBusService messageBusService,
            ILogger<AutomationRegistryService> logger)
        {
            _packageManagerService = packageManagerService ?? throw new ArgumentNullException(nameof(packageManagerService));
            _pythonScriptHostFactoryService = pythonScriptHostFactoryService ?? throw new ArgumentNullException(nameof(pythonScriptHostFactoryService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Start()
        {
            foreach (var automationUid in GetAutomationUids())
            {
                TryInitializeAutomation(automationUid);
            }
        }

        public List<string> GetAutomationUids()
        {
            return _storageService.EnumeratureDirectories("*", AutomationsDirectory);
        }

        public AutomationConfiguration ReadAutomationConfiguration(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            if (!_storageService.TryRead(out AutomationConfiguration configuration, AutomationsDirectory, uid, DefaultFilenames.Configuration))
            {
                throw new AutomationNotFoundException(uid);
            }

            return configuration;
        }

        public void WriteAutomationConfiguration(string uid, AutomationConfiguration configuration)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            _storageService.Write(configuration, AutomationsDirectory, uid, DefaultFilenames.Configuration);
        }

        public void DeleteAutomation(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _storageService.DeleteDirectory(AutomationsDirectory, uid);
        }

        public List<AutomationInstance> GetAutomations()
        {
            lock (_automations)
            {
                return new List<AutomationInstance>(_automations.Values);
            }
        }

        public AutomationInstance GetAutomation(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_automations)
            {
                if (!_automations.TryGetValue(uid, out var automation))
                {
                    throw new AutomationNotFoundException(uid);
                }

                return automation;
            }
        }

        public void ActivateAutomation(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            GetAutomation(uid).Activate();
        }

        public void DeactivateAutomation(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            GetAutomation(uid).Deactivate();
        }

        public object GetAutomationSetting(string automationUid, string settingUid, object defaultValue = null)
        {
            if (automationUid == null) throw new ArgumentNullException(nameof(automationUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            var automation = GetAutomation(automationUid);
            return automation.Settings.GetValueOrDefault(settingUid, defaultValue);
        }

        public void SetAutomationSetting(string automationUid, string settingUid, object value)
        {
            if (automationUid == null) throw new ArgumentNullException(nameof(automationUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            var automation = GetAutomation(automationUid);
            automation.Settings.TryGetValue(settingUid, out var oldValue);

            if (Equals(oldValue, value))
            {
                return;
            }

            automation.Settings[settingUid] = value;

            _storageService.Write(automation.Settings, AutomationsDirectory, automation.Uid, DefaultFilenames.Settings);
            _messageBusService.Publish(new WirehomeDictionary
            {
                ["type"] = "automation_registry.event.setting_changed",
                ["automation_uid"] = automationUid,
                ["setting_uid"] = settingUid,
                ["old_value"] = oldValue,
                ["new_value"] = value,
                ["timestamp"] = DateTimeOffset.Now.ToString("O")
            });
        }
        
        public void RemoveAutomationSetting(string automationUid, string settingUid)
        {
            if (automationUid == null) throw new ArgumentNullException(nameof(automationUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            var automation = GetAutomation(automationUid);
            if (!automation.Settings.TryRemove(settingUid, out var value))
            {
                return;
            }

            _storageService.Write(automation.Settings, AutomationsDirectory, automation.Uid, DefaultFilenames.Settings);

            _messageBusService.Publish(new WirehomeDictionary
            {
                ["type"] = "automation_registry.event.setting_removed",
                ["automation_uid"] = automationUid,
                ["setting_uid"] = settingUid,
                ["value"] = value,
                ["timestamp"] = DateTimeOffset.Now.ToString("O")
            });
        }

        public void TryInitializeAutomation(string uid)
        {
            try
            {
                if (!_storageService.TryRead(out AutomationConfiguration configuration, AutomationsDirectory, uid, DefaultFilenames.Configuration))
                {
                    throw new AutomationNotFoundException(uid);
                }

                if (!configuration.IsEnabled)
                {
                    _logger.LogInformation($"Automation '{uid}' not initialized because it is disabled.");
                    return;
                }

                if (!_storageService.TryRead(out WirehomeDictionary settings, AutomationsDirectory, uid, DefaultFilenames.Settings))
                {
                    settings = new WirehomeDictionary();
                }

                _logger.LogInformation($"Initializing automation '{uid}'.");
                var automation = CreateAutomation(uid, configuration, settings);
                automation.Initialize();
                _logger.LogInformation($"Automation '{uid}' initialized.");

                lock (_automations)
                {
                    if (_automations.TryGetValue(uid, out var existingAutomation))
                    {
                        _logger.LogInformation($"Deactivating automation '{uid}'.");
                        existingAutomation.Deactivate();
                        _logger.LogInformation($"Automation '{uid}' deactivated.");
                    }

                    _automations[uid] = automation;

                    _logger.LogInformation($"Activating automation '{uid}'.");
                    automation.Activate();
                    _logger.LogInformation($"Automation '{uid}' activated.");
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while initializing automation '{uid}'.");
            }
        }

        private AutomationInstance CreateAutomation(string uid, AutomationConfiguration configuration, WirehomeDictionary settings)
        {
            var package = _packageManagerService.LoadPackage(configuration.Logic.Uid);
            var scriptHost = _pythonScriptHostFactoryService.CreateScriptHost(_logger, new AutomationPythonProxy(uid, this));

            scriptHost.Initialize(package.Script);

            var context = new WirehomeDictionary
            {
                ["automation_uid"] = uid,
                ["logic_id"] = configuration.Logic.Uid.Id,
                ["logic_version"] = configuration.Logic.Uid.Version
            };

            // TODO: Remove scope as soon as all automations are migrated.
            scriptHost.SetVariable("scope", context);
            scriptHost.SetVariable("context", context);

            foreach (var variable in configuration.Logic.Variables)
            {
                scriptHost.SetVariable(variable.Key, variable.Value);
            }

            var automation = new AutomationInstance(uid, scriptHost);
            foreach (var setting in settings)
            {
                automation.Settings[setting.Key] = setting.Value;
            }

            return automation;
        }
    }
}
