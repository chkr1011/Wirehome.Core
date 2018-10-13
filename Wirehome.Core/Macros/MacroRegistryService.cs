using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Macros.Configuration;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Model;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Macros
{
    public class MacroRegistryService
    {
        private readonly Dictionary<string, Macro> _macros = new Dictionary<string, Macro>();

        private readonly StorageService _storageService;
        private readonly MessageBusService _messageBusService;
        private readonly SystemStatusService _systemStatusService;
        private readonly ILogger _logger;

        public MacroRegistryService(
            StorageService storageService,
            MessageBusService messageBusService,
            SystemStatusService systemStatusService,
            ILoggerFactory loggerFactory)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _systemStatusService = systemStatusService ?? throw new ArgumentNullException(nameof(systemStatusService));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<MacroRegistryService>();

            if (systemStatusService == null) throw new ArgumentNullException(nameof(systemStatusService));
            systemStatusService.Set("macros.macros_count", () => 0);
        }

        public void Start()
        {
            lock (_macros)
            {
                Load();
            }

            AttachToMessageBus();

            _systemStatusService.Set("macros.macros_count", () => _macros.Count);
        }

        private void AttachToMessageBus()
        {
            _messageBusService.Subscribe("macros.execute", new WirehomeDictionary().WithType("macros.execute"), OnExecuteMacroBusMessage);
        }

        private void OnExecuteMacroBusMessage(WirehomeDictionary properties)
        {

        }

        private void Load()
        {
            _macros.Clear();

            if (_storageService.TryRead(out Dictionary<string, MacroConfiguration> configurations, "Macros.json"))
            {
                foreach (var configuration in configurations)
                {
                    if (configuration.Value.IsEnabled)
                    {
                        TryInitializeMacro(configuration.Key, configuration.Value);
                    }
                }
            }
        }

        private bool TryInitializeMacro(string uid, MacroConfiguration configuration)
        {
            try
            {
                return true;
            }
            catch (Exception exception)
            {
                return false;
            }
        }

        public WirehomeDictionary ExecuteMacro(string uid)
        {
            return null;
        }
    }
}
