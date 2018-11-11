using System;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components.Adapters;
using Wirehome.Core.Components.Configuration;
using Wirehome.Core.Components.Logic;
using Wirehome.Core.Model;
using Wirehome.Core.Python;
using Wirehome.Core.Repository;

namespace Wirehome.Core.Components
{
    public class ComponentInitializer
    {
        private readonly ComponentRegistryService _componentRegistryService;
        private readonly PythonEngineService _pythonEngineService;
        private readonly RepositoryService _repositoryService;
        private readonly ILoggerFactory _loggerFactory;

        public ComponentInitializer(
            ComponentRegistryService componentRegistryService,
            PythonEngineService pythonEngineService,
            RepositoryService repositoryService,
            ILoggerFactory loggerFactory)
        {
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
            _pythonEngineService = pythonEngineService ?? throw new ArgumentNullException(nameof(pythonEngineService));
            _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public void InitializeComponent(Component component, ComponentConfiguration configuration)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var context = new WirehomeDictionary
            {
                ["component_uid"] = component.Uid
            };

            var adapterEntity = _repositoryService.LoadEntity(configuration.Logic.Adapter.Uid);

            var adapter = new ScriptComponentAdapter(_pythonEngineService, _componentRegistryService, _loggerFactory);
            adapter.Initialize(component.Uid, adapterEntity.Script);

            if (configuration.Logic.Adapter.Variables != null)
            {
                foreach (var parameter in configuration.Logic.Adapter.Variables)
                {
                    adapter.SetVariable(parameter.Key, parameter.Value);
                }
            }

            context["adapter_uid"] = adapterEntity.Uid.ToString();
            context["adapter_id"] = adapterEntity.Uid.Id;
            context["adapter_version"] = adapterEntity.Uid.Version;

            if (string.IsNullOrEmpty(configuration.Logic.Uid?.Id))
            {
                component.SetLogic(new EmptyLogic(adapter));
            }
            else
            {
                var logicEntity = _repositoryService.LoadEntity(configuration.Logic.Uid);

                var logic = new ScriptComponentLogic(_pythonEngineService, _componentRegistryService, _loggerFactory);
                adapter.MessagePublishedCallback = message => logic.ProcessAdapterMessage(message);
                logic.AdapterMessagePublishedCallback = message => adapter.ProcessMessage(message);

                logic.Initialize(component.Uid, logicEntity.Script);

                if (configuration.Logic.Variables != null)
                {
                    foreach (var parameter in configuration.Logic.Variables)
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
