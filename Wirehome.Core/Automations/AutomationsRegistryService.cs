using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Automations.Configuration;
using Wirehome.Core.Automations.Exceptions;
using Wirehome.Core.Model;
using Wirehome.Core.Python;
using Wirehome.Core.Repositories;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Automations
{
    public class AutomationsRegistryService
    {
        private readonly Dictionary<string, Automation> _automations = new Dictionary<string, Automation>();

        private readonly RepositoryService _repositoryService;
        private readonly PythonEngineService _pythonEngineService;
        private readonly StorageService _storageService;
        private readonly ILogger _logger;

        public AutomationsRegistryService(
            RepositoryService repositoryService,
            PythonEngineService pythonEngineService,
            StorageService storageService,
            ILoggerFactory loggerFactory)
        {
            _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
            _pythonEngineService = pythonEngineService ?? throw new ArgumentNullException(nameof(pythonEngineService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _logger = loggerFactory.CreateLogger<AutomationsRegistryService>();
        }

        public void Start()
        {
            lock (_automations)
            {
                Load();
            }
        }

        public Automation GetAutomation(string uid)
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

        public void InitializeAutomation(string uid, AutomationConfiguration configuration)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            if (!configuration.IsEnabled)
            {
                _logger.Log(LogLevel.Information, $"Automation '{uid}' not initialized because it is disabled.");
                return;
            }

            var repositoryEntitySource = _repositoryService.LoadEntity(RepositoryType.Automations, configuration.Logic.Uid);

            var scriptHost = _pythonEngineService.CreateScriptHost(_logger);
            scriptHost.Initialize(repositoryEntitySource.Script);

            var scope = new WirehomeDictionary
            {
                ["automation_uid"] = uid,
                ["logic_id"] = configuration.Logic.Uid.Id,
                ["logic_version"] = configuration.Logic.Uid.Version
            };

            scriptHost.SetVariable("scope", scope);
            foreach (var variable in configuration.Logic.Variables)
            {
                scriptHost.SetVariable(variable.Key, variable.Value);
            }

            var automationInstance = new Automation(scriptHost);
            automationInstance.Initialize();

            lock (_automations)
            {
                if (_automations.TryGetValue(uid, out var existingAutomationInstance))
                {
                    existingAutomationInstance.Deactivate();
                }

                automationInstance.Activate();

                _automations[uid] = automationInstance;
            }

            _logger.Log(LogLevel.Information, $"Automation '{uid}' initialized.");
        }

        private void Load()
        {
            _automations.Clear();

            var configurationFiles = _storageService.EnumerateFiles("Configuration.json", "Automations");
            foreach (var configurationFile in configurationFiles)
            {
                if (_storageService.TryRead(out AutomationConfiguration configuration, "Automations", configurationFile))
                {
                    var uid = Path.GetDirectoryName(configurationFile);

                    TryInitializeAutomation(uid, configuration);
                }
            }
        }

        private void TryInitializeAutomation(string uid, AutomationConfiguration configuration)
        {
            try
            {
                InitializeAutomation(uid, configuration);
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, exception, $"Error while initializing automation '{uid}'.");
            }
        }
    }
}
