using System;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components.Adapters;
using Wirehome.Core.Components.Configuration;
using Wirehome.Core.Components.Logic;
using Wirehome.Core.Constants;
using Wirehome.Core.Model;
using Wirehome.Core.Python;
using Wirehome.Core.Repository;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Components
{
    public class ComponentInitializer
    {
        private readonly ComponentRegistryService _componentRegistryService;
        private readonly PythonEngineService _pythonEngineService;
        private readonly StorageService _storageService;
        private readonly RepositoryService _repositoryService;
        private readonly ILoggerFactory _loggerFactory;

        public ComponentInitializer(
            ComponentRegistryService componentRegistryService,
            PythonEngineService pythonEngineService,
            StorageService storageService,
            RepositoryService repositoryService,
            ILoggerFactory loggerFactory)
        {
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
            _pythonEngineService = pythonEngineService ?? throw new ArgumentNullException(nameof(pythonEngineService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public void InitializeComponent(Component component, ComponentConfiguration configuration)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            InitializeSettings(component);
            InitializeLogic(component, configuration.Logic);
            
            component.ProcessMessage(new WirehomeDictionary().WithType(ControlType.Initialize));
        }

        private void InitializeSettings(Component component)
        {
            if (_storageService.TryRead(out WirehomeDictionary settings, "Components", component.Uid, "Settings.json"))
            {
                foreach (var setting in settings)
                {
                    component.Settings[setting.Key] = setting.Value;
                }
            }
        }

        private void InitializeLogic(Component component, ComponentLogicConfiguration configuration)
        {
            var context = new WirehomeDictionary
            {
                ["component_uid"] = component.Uid
            };

            var adapterEntity = _repositoryService.LoadEntity(configuration.Adapter.Uid);

            var adapter = new ScriptComponentAdapter(_pythonEngineService, _componentRegistryService, _loggerFactory);
            adapter.Initialize(component.Uid, adapterEntity.Script);

            if (configuration.Adapter.Variables != null)
            {
                foreach (var parameter in configuration.Adapter.Variables)
                {
                    adapter.SetVariable(parameter.Key, parameter.Value);
                }
            }

            context["adapter_uid"] = adapterEntity.Uid.ToString();
            context["adapter_id"] = adapterEntity.Uid.Id;
            context["adapter_version"] = adapterEntity.Uid.Version;
            
            if (string.IsNullOrEmpty(configuration.Uid?.Id))
            {
                component.SetLogic(new EmptyLogic(adapter));
            }
            else
            {
                var logicEntity = _repositoryService.LoadEntity(configuration.Uid);

                var logic = new ScriptComponentLogic(_pythonEngineService, _componentRegistryService, _loggerFactory);
                adapter.MessagePublishedCallback = message => logic.ProcessAdapterMessage(message);
                logic.AdapterMessagePublishedCallback = message => adapter.ProcessMessage(message);

                logic.Initialize(component.Uid, logicEntity.Script);

                if (configuration.Variables != null)
                {
                    foreach (var parameter in configuration.Variables)
                    {
                        logic.SetVariable(parameter.Key, parameter.Value);
                    }
                }

                context["logic_uid"] = logicEntity.Uid.ToString();
                context["logic_id"] = logicEntity.Uid.Id;
                context["logic_version"] = logicEntity.Uid.Version;

                // TODO: Remove "scope" as soon as it is migrated.
                logic.SetVariable("scope", context);
                logic.SetVariable("context", context);
                component.SetLogic(logic);
            }

            // TODO: Remove "scope" as soon as it is migrated.
            adapter.SetVariable("scope", context);
            adapter.SetVariable("context", context);
        }
    }
}
