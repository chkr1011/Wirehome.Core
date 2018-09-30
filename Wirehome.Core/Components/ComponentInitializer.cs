using System;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components.Adapters;
using Wirehome.Core.Components.Configuration;
using Wirehome.Core.Components.Logic;
using Wirehome.Core.Constants;
using Wirehome.Core.Model;
using Wirehome.Core.Python;
using Wirehome.Core.Repositories;
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
            InitializeSettings(component);
            var logic = InitializeLogic(component, configuration.Logic);
            component.SetLogic(logic);

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

        private IComponentLogic InitializeLogic(Component component, ComponentLogicConfiguration configuration)
        {
            var adapter = InitializeAdapter(component, configuration.Adapter);

            if (string.IsNullOrEmpty(configuration.Adapter?.Uid?.Id))
            {
                return new EmptyLogic(adapter);
            }

            var repositoryEntity = _repositoryService.LoadEntity(RepositoryType.ComponentLogics, configuration.Uid);

            var logic = new ScriptComponentLogic(_pythonEngineService, _componentRegistryService, _loggerFactory);
            adapter.MessagePublishedCallback = message => logic.ProcessAdapterMessage(message);
            logic.AdapterMessagePublishedCallback = message => adapter.ProcessMessage(message);

            logic.Initialize(component.Uid, repositoryEntity.Script);

            if (configuration.Variables != null)
            {
                foreach (var parameter in configuration.Variables)
                {
                    logic.SetVariable(parameter.Key, parameter.Value);
                }
            }

            return logic;
        }

        private IComponentAdapter InitializeAdapter(Component component, ComponentAdapterConfiguration configuration)
        {
            var repositoryEntity = _repositoryService.LoadEntity(RepositoryType.ComponentAdapters, configuration.Uid);

            var adapter = new ScriptComponentAdapter(_pythonEngineService, _componentRegistryService, _loggerFactory);
            adapter.Initialize(component.Uid, repositoryEntity.Script);

            if (configuration.Variables != null)
            {
                foreach (var parameter in configuration.Variables)
                {
                    adapter.SetVariable(parameter.Key, parameter.Value);
                }
            }

            return adapter;
        }
    }
}
