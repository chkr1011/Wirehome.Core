using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Wirehome.Core.App;
using Wirehome.Core.Constants;
using Wirehome.Core.Contracts;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Storage;

namespace Wirehome.Core.GlobalVariables;

public sealed class GlobalVariablesService : WirehomeCoreService
{
    const string GlobalVariablesFilename = "GlobalVariables.json";
    readonly AppService _appService;
    readonly ILogger _logger;
    readonly MessageBusService _messageBusService;
    readonly StorageService _storageService;

    readonly Dictionary<string, object> _variables = new();

    public GlobalVariablesService(AppService appService, StorageService storageService, MessageBusService messageBusService, ILogger<GlobalVariablesService> logger)
    {
        _appService = appService ?? throw new ArgumentNullException(nameof(appService));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void DeleteValue(string uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        lock (_variables)
        {
            if (!_variables.Remove(uid))
            {
                return;
            }
        }

        var busMessage = new Dictionary<object, object>
        {
            ["type"] = "global_variables.event.value_deleted",
            ["uid"] = uid
        };

        _logger.LogInformation($"Global variable '{uid}' removed.");
        _messageBusService.Publish(busMessage);
    }

    public object GetValue(string uid, object defaultValue = null)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        lock (_variables)
        {
            if (!_variables.TryGetValue(uid, out var value))
            {
                return defaultValue;
            }

            return value;
        }
    }

    public Dictionary<string, object> GetValues()
    {
        var result = new Dictionary<string, object>();
        lock (_variables)
        {
            foreach (var variable in _variables)
            {
                result[variable.Key] = variable.Value;
            }
        }

        return result;
    }

    public void SetValue(string uid, object value)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        object oldValue;
        lock (_variables)
        {
            if (_variables.TryGetValue(uid, out oldValue))
            {
                if (Equals(oldValue, value))
                {
                    return;
                }
            }

            _variables[uid] = value;

            Save();
        }

        var busMessage = new Dictionary<object, object>
        {
            ["type"] = "global_variables.event.value_set",
            ["uid"] = uid,
            ["old_value"] = oldValue,
            ["new_value"] = value
        };

        _logger.LogInformation($"Global variable '{uid}' changed to '{value}'.");
        _messageBusService.Publish(busMessage);
    }

    public bool ValueExists(string uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        lock (_variables)
        {
            return _variables.ContainsKey(uid);
        }
    }

    protected override void OnStart()
    {
        _appService.RegisterStatusProvider("globalVariables", GetValues);

        lock (_variables)
        {
            Load();

            RegisterValue(GlobalVariableUids.AppPackageUid, "wirehome.app@1.0.0");
            RegisterValue(GlobalVariableUids.ConfiguratorPackageUid, "wirehome.configurator@1.0.0");
            RegisterValue(GlobalVariableUids.LanguageCode, "en");
        }
    }

    void Load()
    {
        if (_storageService.TryReadSerializedValue(out Dictionary<string, object> globalVariables, GlobalVariablesFilename))
        {
            if (globalVariables == null)
            {
                return;
            }

            foreach (var variable in globalVariables)
            {
                _variables[variable.Key] = variable.Value;
            }
        }
    }

    void RegisterValue(string uid, object value)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        if (!_variables.ContainsKey(uid))
        {
            _variables.Add(uid, value);
            Save();
        }
    }

    void Save()
    {
        _storageService.WriteSerializedValue(_variables, GlobalVariablesFilename);
    }
}