using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Contracts;
using Wirehome.Core.Python;
using Wirehome.Core.Storage;

namespace Wirehome.Core.System.StartupScripts;

public sealed class StartupScriptsService : WirehomeCoreService
{
    const string StartupScriptsDirectory = "StartupScripts";

    readonly ILogger _logger;
    readonly PythonScriptHostFactoryService _pythonScriptHostFactoryService;

    readonly List<StartupScriptInstance> _scripts = new();
    readonly StorageService _storageService;

    public StartupScriptsService(StorageService storageService,
        SystemService systemService,
        PythonScriptHostFactoryService pythonScriptHostFactoryService,
        ILogger<StartupScriptsService> logger)
    {
        if (systemService == null)
        {
            throw new ArgumentNullException(nameof(systemService));
        }

        systemService.StartupCompleted += (s, e) => { OnStartupCompleted(); };

        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _pythonScriptHostFactoryService = pythonScriptHostFactoryService ?? throw new ArgumentNullException(nameof(pythonScriptHostFactoryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void DeleteStartupScript(string uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        _storageService.DeletePath(StartupScriptsDirectory, uid);
    }

    public List<StartupScriptInstance> GetStartupScripts()
    {
        lock (_scripts)
        {
            return new List<StartupScriptInstance>(_scripts);
        }
    }

    public List<string> GetStartupScriptUids()
    {
        return _storageService.EnumerateDirectories("*", StartupScriptsDirectory);
    }

    public string ReadStartupScriptCode(string uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        if (_storageService.TryReadRawText(out var scriptCode, StartupScriptsDirectory, uid, DefaultFileNames.Script))
        {
            return scriptCode;
        }

        throw new StartupScriptNotFoundException(uid);
    }

    public StartupScriptConfiguration ReadStartupScriptConfiguration(string uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        if (!_storageService.TryReadSerializedValue(out StartupScriptConfiguration configuration, StartupScriptsDirectory, uid, DefaultFileNames.Configuration))
        {
            throw new StartupScriptNotFoundException(uid);
        }

        return configuration;
    }

    public void WriteStartupScripConfiguration(string uid, StartupScriptConfiguration configuration)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _storageService.WriteSerializedValue(configuration, StartupScriptsDirectory, uid, DefaultFileNames.Configuration);
    }

    public void WriteStartupScriptCode(string uid, string scriptCode)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        if (scriptCode == null)
        {
            throw new ArgumentNullException(nameof(scriptCode));
        }

        _storageService.WriteRawText(scriptCode, StartupScriptsDirectory, uid, DefaultFileNames.Script);
    }

    protected override void OnStart()
    {
        foreach (var startupScriptUid in GetStartupScriptUids())
        {
            TryInitializeStartupScript(startupScriptUid);
        }
    }

    StartupScriptInstance CreateStartupScriptInstance(string uid, StartupScriptConfiguration configuration)
    {
        if (!_storageService.TryReadRawText(out var scriptCode, StartupScriptsDirectory, uid, DefaultFileNames.Script))
        {
            throw new InvalidOperationException("Script file not found.");
        }

        var scriptHost = _pythonScriptHostFactoryService.CreateScriptHost();
        scriptHost.Compile(scriptCode);

        return new StartupScriptInstance(uid, configuration, scriptHost);
    }

    void OnConfigurationLoaded()
    {
        lock (_scripts)
        {
            TryExecuteFunction("on_configuration_loaded");
        }
    }

    void OnServicesInitialized()
    {
        lock (_scripts)
        {
            TryExecuteFunction("on_services_initialized");
        }
    }

    void OnStartupCompleted()
    {
        lock (_scripts)
        {
            TryExecuteFunction("on_startup_completed");
        }
    }

    void TryExecuteFunction(string name)
    {
        foreach (var scriptInstance in _scripts)
        {
            TryExecuteFunction(scriptInstance, name);
        }
    }

    void TryExecuteFunction(StartupScriptInstance startupScriptInstance, string functionName)
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

    void TryInitializeStartupScript(string uid)
    {
        try
        {
            if (!_storageService.TryReadSerializedValue(out StartupScriptConfiguration configuration, StartupScriptsDirectory, uid, DefaultFileNames.Configuration))
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
}