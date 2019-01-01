using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components;
using Wirehome.Core.Constants;
using Wirehome.Core.Macros.Configuration;
using Wirehome.Core.Model;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Models;

namespace Wirehome.Core.Macros
{
    public class MacroInstance
    {
        private readonly ComponentRegistryService _componentRegistryService;
        private readonly ILogger _logger;

        private readonly PythonScriptHost _scriptHost;
        private readonly List<MacroActionConfiguration> _actions;

        public MacroInstance(string uid, List<MacroActionConfiguration> actions, PythonScriptHost scriptHost, ComponentRegistryService componentRegistryService, ILogger logger)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
            _actions = actions;
            _scriptHost = scriptHost;

            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Uid { get; }

        public ConcurrentWirehomeDictionary Settings { get; } = new ConcurrentWirehomeDictionary();

        public WirehomeDictionary Destroy()
        {
            if (_scriptHost == null)
            {
                return new WirehomeDictionary().WithType(ControlType.Success);
            }

            if (_scriptHost.FunctionExists("destroy"))
            {
                var result = _scriptHost.InvokeFunction("destroy") as WirehomeDictionary;
                return result;
            }

            return new WirehomeDictionary().WithType(ControlType.Success);
        }

        public WirehomeDictionary Initialize()
        {
            if (_scriptHost == null)
            {
                return new WirehomeDictionary().WithType(ControlType.Success);
            }

            var result = _scriptHost.InvokeFunction("initialize") as WirehomeDictionary;
            return result;
        }

        public WirehomeDictionary TryExecute()
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
                    var scriptResult = _scriptHost.InvokeFunction("execute");
                    if (scriptResult is WirehomeDictionary wirehomeDictionary)
                    {
                        return wirehomeDictionary;
                    }
                }
                catch (Exception exception)
                {
                    return new ExceptionPythonModel(exception).ConvertToWirehomeDictionary();
                }                
            }

            return new WirehomeDictionary().WithType(ControlType.Success);
        }

        private void TryExecuteAction(MacroActionConfiguration action)
        {
            try
            {
                if (action is SendComponentMessageMacroActionConfiguration sendComponentMessageMacroAction)
                {
                    _componentRegistryService.ProcessComponentMessage(
                        sendComponentMessageMacroAction.ComponentUid,
                        sendComponentMessageMacroAction.Message);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while executing action of macro '{Uid}'.");
            }
        }
    }
}