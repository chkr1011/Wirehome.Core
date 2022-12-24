using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Wirehome.Core.Components;
using Wirehome.Core.Constants;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Macros.Configuration;
using Wirehome.Core.Macros.Exceptions;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Macros;

public sealed class MacroRegistryService : WirehomeCoreService
{
    const string MacrosDirectory = "Macros";
    readonly ComponentRegistryService _componentRegistryService;
    readonly ILogger _logger;

    readonly Dictionary<string, MacroInstance> _macros = new();
    readonly MessageBusService _messageBusService;

    readonly MacroRegistryMessageBusWrapper _messageBusWrapper;
    readonly StorageService _storageService;

    public MacroRegistryService(StorageService storageService,
        MessageBusService messageBusService,
        SystemStatusService systemStatusService,
        ComponentRegistryService componentRegistryService,
        ILogger<MacroRegistryService> logger)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
        _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (systemStatusService == null)
        {
            throw new ArgumentNullException(nameof(systemStatusService));
        }

        systemStatusService.Set("macro_registry.count", () => _macros.Count);

        _messageBusWrapper = new MacroRegistryMessageBusWrapper(_messageBusService);
    }

    public void DeleteMacro(string uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        _storageService.DeletePath(MacrosDirectory, uid);
    }

    public IDictionary<object, object> ExecuteMacro(string uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        MacroInstance macroInstance;
        lock (_macros)
        {
            if (!_macros.TryGetValue(uid, out macroInstance))
            {
                return new Dictionary<object, object>
                {
                    ["type"] = WirehomeMessageType.ParameterInvalidException
                };
            }
        }

        var result = macroInstance.TryExecute();
        _messageBusWrapper.PublishMacroExecutedBusMessage(uid, result);
        return result;
    }

    public MacroInstance GetMacro(string uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        lock (_macros)
        {
            if (!_macros.TryGetValue(uid, out var macro))
            {
                throw new MacroNotFoundException(uid);
            }

            return macro;
        }
    }

    public object GetMacroSetting(string macroUid, string settingUid, object defaultValue = null)
    {
        if (macroUid == null)
        {
            throw new ArgumentNullException(nameof(macroUid));
        }

        if (settingUid == null)
        {
            throw new ArgumentNullException(nameof(settingUid));
        }

        var macro = GetMacro(macroUid);

        if (macro.Settings.TryGetValue(settingUid, out var value))
        {
            return value;
        }

        return defaultValue;
    }

    public List<string> GetMacroUids()
    {
        return _storageService.EnumerateDirectories("*", MacrosDirectory);
    }

    public void InitializeMacro(string uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        try
        {
            if (!_storageService.TryReadSerializedValue(out MacroConfiguration configuration, MacrosDirectory, uid, DefaultFileNames.Configuration))
            {
                throw new MacroNotFoundException(uid);
            }

            if (!configuration.IsEnabled)
            {
                _logger.LogInformation($"Macro '{uid}' not initialized because it is disabled.");
                return;
            }

            var actions = new List<MacroActionConfiguration>();
            foreach (var actionConfiguration in configuration.Actions)
            {
                actions.Add(ParseActionConfiguration(actionConfiguration));
            }

            var macroInstance = new MacroInstance(uid, actions, null, _componentRegistryService, _logger);
            macroInstance.Initialize();

            lock (_macros)
            {
                if (_macros.TryGetValue(uid, out var existingMacro))
                {
                    existingMacro.Destroy();
                }

                _macros[uid] = macroInstance;
            }

            _logger.LogInformation($"Macro '{uid}' initialized successfully.");
        }
        catch
        {
            lock (_macros)
            {
                _macros.Remove(uid, out _);
            }

            throw;
        }
    }

    public MacroConfiguration ReadMacroConfiguration(string uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        if (!_storageService.TryReadSerializedValue(out MacroConfiguration configuration, MacrosDirectory, uid, DefaultFileNames.Configuration))
        {
            throw new MacroNotFoundException(uid);
        }

        return configuration;
    }

    public object RemoveMacroSetting(string macroUid, string settingUid)
    {
        if (macroUid == null)
        {
            throw new ArgumentNullException(nameof(macroUid));
        }

        if (settingUid == null)
        {
            throw new ArgumentNullException(nameof(settingUid));
        }

        var macro = GetMacro(macroUid);
        macro.Settings.Remove(settingUid, out var value);

        _storageService.WriteSerializedValue(macro.Settings, MacrosDirectory, macroUid, DefaultFileNames.Settings);
        _messageBusWrapper.PublishSettingRemovedBusMessage(macroUid, settingUid, value);

        return value;
    }

    public void SetMacroSetting(string macroUid, string settingUid, object value)
    {
        if (macroUid == null)
        {
            throw new ArgumentNullException(nameof(macroUid));
        }

        if (settingUid == null)
        {
            throw new ArgumentNullException(nameof(settingUid));
        }

        var macro = GetMacro(macroUid);
        macro.Settings.TryGetValue(settingUid, out var oldValue);

        if (Equals(oldValue, value))
        {
            return;
        }

        macro.Settings[settingUid] = value;

        _storageService.WriteSerializedValue(macro.Settings, MacrosDirectory, macro.Uid, DefaultFileNames.Settings);
        _messageBusWrapper.PublishSettingChangedBusMessage(macroUid, settingUid, oldValue, value);
    }

    public void TryInitializeMacro(string uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        try
        {
            InitializeMacro(uid);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Error while initializing macro '{uid}'.");
        }
    }

    public void WriteMacroConfiguration(string uid, MacroConfiguration configuration)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _storageService.WriteSerializedValue(configuration, MacrosDirectory, uid, DefaultFileNames.Configuration);
    }

    protected override void OnStart()
    {
        foreach (var macroUid in GetMacroUids())
        {
            TryInitializeMacro(macroUid);
        }

        AttachToMessageBus();
    }

    void AttachToMessageBus()
    {
        var filter = new Dictionary<object, object>
        {
            ["type"] = "macro_registry.execute"
        };

        _messageBusService.Subscribe("macro_registry.execute", filter, OnExecuteMacroBusMessage);
    }

    void OnExecuteMacroBusMessage(IDictionary<object, object> busMessage)
    {
    }

    MacroActionConfiguration ParseActionConfiguration(JObject actionConfiguration)
    {
        if (actionConfiguration["type"].Value<string>() == "send_component_message")
        {
            return new SendComponentMessageMacroActionConfiguration
            {
                ComponentUid = actionConfiguration["component_uid"].Value<string>(),
                Message = actionConfiguration["message"].ToObject<IDictionary<object, object>>()
            };
        }

        throw new NotSupportedException("Macro action type not supported.");
    }
}