using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components;
using Wirehome.Core.Constants;
using Wirehome.Core.Macros.Configuration;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Models;

namespace Wirehome.Core.Macros;

public class MacroInstance
{
    readonly List<MacroActionConfiguration> _actions;
    readonly ComponentRegistryService _componentRegistryService;
    readonly ILogger _logger;

    readonly PythonScriptHost _scriptHost;

    public MacroInstance(string uid, List<MacroActionConfiguration> actions, PythonScriptHost scriptHost, ComponentRegistryService componentRegistryService, ILogger logger)
    {
        Uid = uid ?? throw new ArgumentNullException(nameof(uid));
        _actions = actions;
        _scriptHost = scriptHost;

        _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ConcurrentDictionary<string, object> Settings { get; } = new();

    public string Uid { get; }

    public IDictionary<object, object> Destroy()
    {
        if (_scriptHost == null)
        {
            return new Dictionary<object, object>
            {
                ["type"] = WirehomeMessageType.Success
            };
        }

        if (_scriptHost.FunctionExists("destroy"))
        {
            return _scriptHost.InvokeFunction("destroy") as IDictionary<object, object>;
        }

        return new Dictionary<object, object>
        {
            ["type"] = WirehomeMessageType.Success
        };
    }

    public IDictionary<object, object> Initialize()
    {
        if (_scriptHost == null)
        {
            return new Dictionary<object, object>
            {
                ["type"] = WirehomeMessageType.Success
            };
        }

        return _scriptHost.InvokeFunction("initialize") as IDictionary<object, object>;
    }

    public IDictionary<object, object> TryExecute()
    {
        if (_actions != null)
        {
            foreach (var action in _actions)
            {
                TryExecuteAction(action);
            }
        }

        if (_scriptHost != null)
        {
            try
            {
                return _scriptHost.InvokeFunction("execute") as IDictionary<object, object>;
            }
            catch (Exception exception)
            {
                return new ExceptionPythonModel(exception).ToDictionary();
            }
        }

        return new Dictionary<object, object>
        {
            ["type"] = WirehomeMessageType.Success
        };
    }

    void TryExecuteAction(MacroActionConfiguration action)
    {
        try
        {
            if (action is SendComponentMessageMacroActionConfiguration sendComponentMessageMacroAction)
            {
                _componentRegistryService.ProcessComponentMessage(sendComponentMessageMacroAction.ComponentUid, sendComponentMessageMacroAction.Message);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Error while executing action of macro '{Uid}'.");
        }
    }
}