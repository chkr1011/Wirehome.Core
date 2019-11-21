using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Wirehome.Core.Components;
using Wirehome.Core.Constants;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Macros.Configuration;
using Wirehome.Core.Macros.Exceptions;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Foundation.Model;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Macros
{
    public class MacroRegistryService : IService
    {
        private const string MacrosDirectory = "Macros";

        private readonly Dictionary<string, MacroInstance> _macros = new Dictionary<string, MacroInstance>();

        private readonly MacroRegistryMessageBusWrapper _messageBusWrapper;
        private readonly StorageService _storageService;
        private readonly MessageBusService _messageBusService;
        private readonly ComponentRegistryService _componentRegistryService;
        private readonly ILogger _logger;

        public MacroRegistryService(
            StorageService storageService,
            MessageBusService messageBusService,
            SystemStatusService systemStatusService,
            ComponentRegistryService componentRegistryService,
            ILogger<MacroRegistryService> logger)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (systemStatusService == null) throw new ArgumentNullException(nameof(systemStatusService));
            systemStatusService.Set("macro_registry.count", () => _macros.Count);

            _messageBusWrapper = new MacroRegistryMessageBusWrapper(_messageBusService);
        }

        public void Start()
        {
            foreach (var macroUid in GetMacroUids())
            {
                TryInitializeMacro(macroUid);
            }

            AttachToMessageBus();
        }

        public List<string> GetMacroUids()
        {
            return _storageService.EnumerateDirectories("*", MacrosDirectory);
        }

        public MacroInstance GetMacro(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_macros)
            {
                if (!_macros.TryGetValue(uid, out var macro))
                {
                    throw new MacroNotFoundException(uid);
                }

                return macro;
            }
        }

        public void WriteMacroConfiguration(string uid, MacroConfiguration configuration)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            _storageService.Write(configuration, MacrosDirectory, uid, DefaultFilenames.Configuration);
        }

        public MacroConfiguration ReadMacroConfiguration(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            if (!_storageService.TryRead(out MacroConfiguration configuration, MacrosDirectory, uid, DefaultFilenames.Configuration))
            {
                throw new MacroNotFoundException(uid);
            }

            return configuration;
        }

        public void DeleteMacro(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _storageService.DeleteDirectory(MacrosDirectory, uid);
        }

        public object GetMacroSetting(string macroUid, string settingUid, object defaultValue = null)
        {
            if (macroUid == null) throw new ArgumentNullException(nameof(macroUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            var macro = GetMacro(macroUid);
            return macro.Settings.GetValueOrDefault(settingUid, defaultValue);
        }

        public void SetMacroSetting(string macroUid, string settingUid, object value)
        {
            if (macroUid == null) throw new ArgumentNullException(nameof(macroUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            var macro = GetMacro(macroUid);
            macro.Settings.TryGetValue(settingUid, out var oldValue);

            if (Equals(oldValue, value))
            {
                return;
            }

            macro.Settings[settingUid] = value;

            _storageService.Write(macro.Settings, MacrosDirectory, macro.Uid, DefaultFilenames.Settings);
            _messageBusWrapper.PublishSettingChangedBusMessage(macroUid, settingUid, oldValue, value);
        }

        public object RemoveMacroSetting(string macroUid, string settingUid)
        {
            if (macroUid == null) throw new ArgumentNullException(nameof(macroUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            var macro = GetMacro(macroUid);
            macro.Settings.Remove(settingUid, out var value);

            _storageService.Write(macro.Settings, MacrosDirectory, macroUid, DefaultFilenames.Settings);
            _messageBusWrapper.PublishSettingRemovedBusMessage(macroUid, settingUid, value);

            return value;
        }

        public void TryInitializeMacro(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            try
            {
                InitializeMacro(uid);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while initializing macro '{uid}'.");
            }
        }

        public void InitializeMacro(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            try
            {
                if (!_storageService.TryRead(out MacroConfiguration configuration, MacrosDirectory, uid, DefaultFilenames.Configuration))
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

        public WirehomeDictionary ExecuteMacro(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            MacroInstance macroInstance;
            lock (_macros)
            {
                if (!_macros.TryGetValue(uid, out macroInstance))
                {
                    return new WirehomeDictionary().WithType(ControlType.ParameterInvalidException);
                }
            }

            var result = macroInstance.TryExecute();
            _messageBusWrapper.PublishMacroExecutedBusMessage(uid, result);
            return result;
        }

        private void AttachToMessageBus()
        {
            _messageBusService.Subscribe("macro_registry.execute", new WirehomeDictionary().WithType("macro_registry.execute"), OnExecuteMacroBusMessage);
        }

        private MacroActionConfiguration ParseActionConfiguration(JObject actionConfiguration)
        {
            if (actionConfiguration["type"].Value<string>() == "send_component_message")
            {
                return new SendComponentMessageMacroActionConfiguration
                {
                    ComponentUid = actionConfiguration["component_uid"].Value<string>(),
                    Message = (WirehomeDictionary)WirehomeConvert.FromJson(actionConfiguration["message"])
                };
            }

            throw new NotSupportedException("Macro action type not supported.");
        }

        private void OnExecuteMacroBusMessage(MessageBusMessage busMessage)
        {
        }
    }
}
