using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            lock (_scripts)
            {
                Load();
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

        public Dictionary<string, StartupScript> GetStartupScripts()
        {
            lock (_scripts)
            {
                var configurationFiles = _storageService.EnumerateFiles("Configuration.json", "StartupScripts");

                var startupScripts = new Dictionary<string, StartupScript>();
                foreach (var configurationFile in configurationFiles)
                {
                    var uid = Path.GetDirectoryName(configurationFile);
                    startupScripts[uid] = GetStartupScript(uid);
                }

                return startupScripts;
            }
        }

        public StartupScript GetStartupScript(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_scripts)
            {
                if (!_storageService.TryRead(out StartupScriptConfiguration configuration, "StartupScripts", uid,
                    "Configuration.json"))
                {
                    throw new StartupScriptNotFoundException(uid);
                }

                return new StartupScript { Configuration = configuration };
            }
        }

        public void SetStartupScriptCode(string uid, string scriptCode)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_scripts)
            {
                _storageService.WriteText(scriptCode, "StartupScripts", uid, "script.py");
            }
        }

        public string GetStartupScriptCode(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_scripts)
            {
                if (_storageService.TryReadText(out var scriptCode, "StartupScripts", uid, "script.py"))
                {
                    return scriptCode;
                }

                throw new StartupScriptNotFoundException(uid);
            }
        }

        public void RemoveStartupScript(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_scripts)
            {
                _storageService.DeleteDirectory("StartupScripts", uid);
            }
        }

        public void CreateStartupScript(string uid, StartupScriptConfiguration configuration)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            lock (_scripts)
            {
                _storageService.Write(configuration, "StartupScripts", uid, "Configuration.json");
            }
        }

        private void TryExecuteFunction(string name)
        {
            foreach (var scriptInstance in _scripts)
            {
                TryExecuteFunction(scriptInstance, name);
            }
        }

        private void TryExecuteFunction(StartupScriptInstance scriptInstance, string name)
        {
            try
            {
                if (!scriptInstance.ScriptHost.FunctionExists(name))
                {
                    return;
                }

                scriptInstance.ScriptHost.InvokeFunction(name);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while executing function '{name}' of startup script '{scriptInstance.Uid}'.");
            }
        }

        private void Load()
        {
            var configurationFiles = _storageService.EnumerateFiles("Configuration.json", "StartupScripts");
            foreach (var configurationFile in configurationFiles.OrderBy(f => f))
            {
                var uid = Path.GetDirectoryName(configurationFile);
                if (!_storageService.TryReadText(out var scriptCode, "StartupScripts", uid, "script.py"))
                {
                    _logger.LogWarning($"Startup script '{uid}' contains no script code.");
                    return;
                }

                if (TryInitializeScript(uid, scriptCode, out var scriptHost))
                {
                    _scripts.Add(new StartupScriptInstance(uid, scriptHost));
                }
            }
        }

        private bool TryInitializeScript(string uid, string scriptCode, out PythonScriptHost scriptHost)
        {
            try
            {
                scriptHost = _pythonEngineService.CreateScriptHost(_logger);
                scriptHost.Initialize(scriptCode);

                if (scriptHost.FunctionExists("initialize"))
                {
                    scriptHost.InvokeFunction("initialize");
                }

                return true;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while initializing startup script '{uid}'.");

                scriptHost = null;
                return false;
            }
        }
    }
}
