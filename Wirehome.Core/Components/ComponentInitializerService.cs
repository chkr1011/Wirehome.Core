using System;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Contracts;
using Wirehome.Core.Python;
using Wirehome.Core.Repository;

namespace Wirehome.Core.Components
{
    public class ComponentInitializerService : IService
    {
        private readonly PythonScriptHostFactoryService _pythonScriptHostFactoryService;
        private readonly RepositoryService _repositoryService;
        private readonly ILoggerFactory _loggerFactory;

        public ComponentInitializerService(
            PythonScriptHostFactoryService pythonScriptHostFactoryService,
            RepositoryService repositoryService,
            ILoggerFactory loggerFactory)
        {
            _pythonScriptHostFactoryService = pythonScriptHostFactoryService ?? throw new ArgumentNullException(nameof(pythonScriptHostFactoryService));
            _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public void Start()
        {
        }

        public ComponentInitializer Create(ComponentRegistryService componentRegistryService)
        {
            if (componentRegistryService == null) throw new ArgumentNullException(nameof(componentRegistryService));

            return new ComponentInitializer(componentRegistryService, _pythonScriptHostFactoryService, _repositoryService, _loggerFactory);
        }
    }
}
