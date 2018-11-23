using System;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components.Adapters;
using Wirehome.Core.Components.Logic;
using Wirehome.Core.Contracts;
using Wirehome.Core.Python;
using Wirehome.Core.Repository;

namespace Wirehome.Core.Components
{
    public class ComponentInitializerService : IService
    {
        private readonly PythonScriptHostFactoryService _pythonScriptHostFactoryService;
        private readonly RepositoryService _repositoryService;
        private readonly ILogger<ScriptComponentLogic> _scriptComponentLogicLogger;
        private readonly ILogger<ScriptComponentAdapter> _scriptComponentAdapterLogger;
        
        public ComponentInitializerService(
            PythonScriptHostFactoryService pythonScriptHostFactoryService,
            RepositoryService repositoryService,
            ILogger<ScriptComponentLogic> scriptComponentLogicLogger,
            ILogger<ScriptComponentAdapter> scriptComponentAdapterLogger)
        {
            _pythonScriptHostFactoryService = pythonScriptHostFactoryService ?? throw new ArgumentNullException(nameof(pythonScriptHostFactoryService));
            _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
            _scriptComponentLogicLogger = scriptComponentLogicLogger ?? throw new ArgumentNullException(nameof(scriptComponentLogicLogger));
            _scriptComponentAdapterLogger = scriptComponentAdapterLogger ?? throw new ArgumentNullException(nameof(scriptComponentAdapterLogger));
        }

        public void Start()
        {
        }

        public ComponentInitializer Create(ComponentRegistryService componentRegistryService)
        {
            if (componentRegistryService == null) throw new ArgumentNullException(nameof(componentRegistryService));

            return new ComponentInitializer(
                componentRegistryService, 
                _pythonScriptHostFactoryService, 
                _repositoryService,
                _scriptComponentLogicLogger,
                _scriptComponentAdapterLogger);
        }
    }
}
