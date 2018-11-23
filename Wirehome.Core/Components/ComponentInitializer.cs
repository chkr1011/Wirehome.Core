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
        private readonly PythonScriptHostFactoryService _pythonScriptHostFactoryService;
        private readonly RepositoryService _repositoryService;
        private readonly ILogger<ScriptComponentLogic> _scriptComponentLogicLogger;
        private readonly ILogger<ScriptComponentAdapter> _scriptComponentAdapterLogger;

        public ComponentInitializer(
            ComponentRegistryService componentRegistryService,
            PythonScriptHostFactoryService pythonScriptHostFactoryService,
            RepositoryService repositoryService,
            ILogger<ScriptComponentLogic> scriptComponentLogicLogger,
            ILogger<ScriptComponentAdapter> scriptComponentAdapterLogger)
        {
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
            _pythonScriptHostFactoryService = pythonScriptHostFactoryService ?? throw new ArgumentNullException(nameof(_pythonScriptHostFactoryService));
            _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
            _scriptComponentLogicLogger = scriptComponentLogicLogger ?? throw new ArgumentNullException(nameof(scriptComponentLogicLogger));
            _scriptComponentAdapterLogger = scriptComponentAdapterLogger ?? throw new ArgumentNullException(nameof(scriptComponentAdapterLogger));
        }

        public void InitializeComponent(Component component, ComponentConfiguration configuration)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var context = new WirehomeDictionary
            {
                ["component_uid"] = component.Uid
            };

            var adapterEntity = _repositoryService.LoadPackage(configuration.Logic.Adapter.Uid);

            var adapter = new ScriptComponentAdapter(_pythonScriptHostFactoryService, _componentRegistryService, _scriptComponentAdapterLogger);
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
                var logicEntity = _repositoryService.LoadPackage(configuration.Logic.Uid);

                var logic = new ScriptComponentLogic(_pythonScriptHostFactoryService, _componentRegistryService, _scriptComponentLogicLogger);
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
