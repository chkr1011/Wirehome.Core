using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Contracts;
using Wirehome.Core.Python;
using Wirehome.Core.Storage;

namespace Wirehome.Core.System.StartupScripts
{
    public class StartupScriptsService : IService
    {
        private const string StartupScriptsDirectory = "StartupScripts";

        private readonly List<StartupScriptInstance> _scripts = new List<StartupScriptInstance>();

        private readonly ILogger _logger;
        private readonly StorageService _storageService;
        private readonly PythonScriptHostFactoryService _pythonScriptHostFactoryService;
        
        public StartupScriptsService(
            StorageService storageService, 
            PythonScriptHostFactoryService pythonScriptHostFactoryService, 
            ILogger<StartupScriptsService> logger)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _pythonScriptHostFactoryService = pythonScriptHostFactoryService ?? throw new ArgumentNullException(nameof(pythonScriptHostFactoryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Start()
        {
            foreach (var startupScriptUid in GetStartupScriptUids())
            {
                TryInitializeStartupScript(startupScriptUid);
            }
        }

        public List<string> GetStartupScriptUids()
        {
            return _storageService.EnumeratureDirectories("*", StartupScriptsDirectory);
        }

        public List<StartupScriptInstance> GetStartupScripts()
        {
            lock (_scripts)
            {
                return new List<StartupScriptInstance>(_scripts);
            }
        }

        public StartupScriptConfiguration ReadStartupScriptConfiguration(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            if (!_storageService.TryRead(out StartupScriptConfiguration configuration, StartupScriptsDirectory, uid, DefaultFilenames.Configuration))
            {
                throw new StartupScriptNotFoundException(uid);
            }

            return configuration;
        }

        public void WriteStartupScripConfiguration(string uid, StartupScriptConfiguration configuration)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            _storageService.Write(configuration, StartupScriptsDirectory, uid, DefaultFilenames.Configuration);
        }

        public void DeleteStartupScript(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _storageService.DeleteDirectory(StartupScriptsDirectory, uid);
        }

        public void WriteStartupScriptCode(string uid, string scriptCode)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (scriptCode == null) throw new ArgumentNullException(nameof(scriptCode));

            _storageService.WriteText(scriptCode, StartupScriptsDirectory, uid, DefaultFilenames.Script);
        }

        public string ReadStartupScriptCode(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            if (_storageService.TryReadText(out var scriptCode, StartupScriptsDirectory, uid, DefaultFilenames.Script))
            {
                return scriptCode;
            }

            throw new StartupScriptNotFoundException(uid);
        }

        public void OnServicesInitialized()
        {
            lock (_scripts)
            {
                TryExecuteFunction("on_services_initialized");
            }
        }

        public void OnConfigurationLoaded()
        {
            lock (_scripts)
            {
                TryExecuteFunction("on_configuration_loaded");
            }
        }

        public void OnStartupCompleted()
        {
            lock (_scripts)
            {
                TryExecuteFunction("on_startup_completed");
            }
        }

        private void TryExecuteFunction(string name)
        {
            foreach (var scriptInstance in _scripts)
            {
                TryExecuteFunction(scriptInstance, name);
            }
        }

        private void TryExecuteFunction(StartupScriptInstance startupScriptInstance, string functionName)
        {
            try
            {
                if (!startupScriptInstance.FunctionExists(functionName))
                {
                    return;
                }

                startupScriptInstance.InvokeFunction(functionName);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while executing function '{functionName}' of startup script '{startupScriptInstance.Uid}'.");
            }
        }

        private void TryInitializeStartupScript(string uid)
        {
            try
            {
                if (!_storageService.TryRead(out StartupScriptConfiguration configuration, StartupScriptsDirectory, uid, DefaultFilenames.Configuration))
                {
                    throw new StartupScriptNotFoundException(uid);
                }

                if (!configuration.IsEnabled)
                {
                    _logger.LogInformation($"Startup script '{uid}' not executed because it is disabled.");
                    return;
                }

                _logger.LogInformation($"Initializing startup script '{uid}'.");
                var startupScriptInstance = CreateStartupScriptInstance(uid, configuration);
                if (startupScriptInstance.FunctionExists("initialize"))
                {
                    startupScriptInstance.InvokeFunction("initialize");
                }

                _logger.LogInformation($"Startup script '{uid}' initialized.");

                lock (_scripts)
                {
                    _scripts.Add(startupScriptInstance);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while initializing startup script '{uid}'.");
            }
        }

        private StartupScriptInstance CreateStartupScriptInstance(string uid, StartupScriptConfiguration configuration)
        {
            if (!_storageService.TryReadText(out var scriptCode, StartupScriptsDirectory, uid, DefaultFilenames.Script))
            {
                throw new InvalidOperationException("Script file not found.");
            }

            var scriptHost = _pythonScriptHostFactoryService.CreateScriptHost(_logger);
            scriptHost.Compile(scriptCode);

            return new StartupScriptInstance(uid, configuration, scriptHost);
        }
    }
}
