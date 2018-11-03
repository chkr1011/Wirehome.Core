using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Python;
using Wirehome.Core.Storage;

namespace Wirehome.Core.System.StartupScripts
{
    public class StartupScriptsService
    {
        private readonly List<StartupScriptInstance> _scripts = new List<StartupScriptInstance>();

        private readonly ILogger _logger;
        private readonly StorageService _storageService;
        private readonly PythonEngineService _pythonEngineService;

        public StartupScriptsService(StorageService storageService, PythonEngineService pythonEngineService, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _pythonEngineService = pythonEngineService ?? throw new ArgumentNullException(nameof(pythonEngineService));
            _logger = loggerFactory.CreateLogger<StartupScriptsService>();
        }

        public void Start()
        {
            var startupScriptDirectories = _storageService.EnumeratureDirectories("*", "StartupScripts");
            foreach (var startupScriptUid in startupScriptDirectories)
            {
                TryInitializeStartupScript(startupScriptUid);
            }
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

        public List<StartupScriptInstance> GetStartupScripts()
        {
            lock (_scripts)
            {
                return new List<StartupScriptInstance>(_scripts);
            }
        }

        public void WriteStartupScriptCode(string uid, string scriptCode)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (scriptCode == null) throw new ArgumentNullException(nameof(scriptCode));

            _storageService.WriteText(scriptCode, "StartupScripts", uid, DefaultFilenames.Script);
        }

        public string ReadStartupScriptCode(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            if (_storageService.TryReadText(out var scriptCode, "StartupScripts", uid, DefaultFilenames.Script))
            {
                return scriptCode;
            }

            throw new StartupScriptNotFoundException(uid);
        }

        public void DeleteStartupScript(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _storageService.DeleteDirectory("StartupScripts", uid);
        }

        public StartupScriptConfiguration ReadStartupScriptConfiguration(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            if (!_storageService.TryRead(out StartupScriptConfiguration configuration, "StartupScripts", uid, DefaultFilenames.Configuration))
            {
                throw new StartupScriptNotFoundException(uid);
            }

            return configuration;
        }

        public void WriteStartupScripConfiguration(string uid, StartupScriptConfiguration configuration)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            _storageService.Write(configuration, "StartupScripts", uid, DefaultFilenames.Configuration);
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
                if (!_storageService.TryRead(out StartupScriptConfiguration configuration, "StartupScripts", uid, DefaultFilenames.Configuration))
                {
                    return;
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

                _scripts.Add(startupScriptInstance);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while initializing startup script '{uid}'.");
            }
        }

        private StartupScriptInstance CreateStartupScriptInstance(string uid, StartupScriptConfiguration configuration)
        {
            if (!_storageService.TryReadText(out var scriptCode, "StartupScripts", uid, DefaultFilenames.Script))
            {
                throw new InvalidOperationException("Script file not found.");
            }

            var scriptHost = _pythonEngineService.CreateScriptHost(_logger);
            scriptHost.Initialize(scriptCode);

            return new StartupScriptInstance(uid, configuration, scriptHost);
        }
    }
}
