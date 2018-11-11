using System;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Python;
using Wirehome.Core.Repository;

namespace Wirehome.Core.Components
{
    public class ComponentInitializerFactory
    {
        private readonly PythonEngineService _pythonEngineService;
        private readonly RepositoryService _repositoryService;
        private readonly ILoggerFactory _loggerFactory;

        public ComponentInitializerFactory(
            PythonEngineService pythonEngineService,
            RepositoryService repositoryService,
            ILoggerFactory loggerFactory)
        {
            _pythonEngineService = pythonEngineService ?? throw new ArgumentNullException(nameof(pythonEngineService));
            _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public ComponentInitializer Create(ComponentRegistryService componentRegistryService)
        {
            if (componentRegistryService == null) throw new ArgumentNullException(nameof(componentRegistryService));

            return new ComponentInitializer(componentRegistryService, _pythonEngineService, _repositoryService, _loggerFactory);
        }
    }
}
