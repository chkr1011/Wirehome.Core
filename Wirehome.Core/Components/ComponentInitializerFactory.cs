using System;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Python;
using Wirehome.Core.Repository;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Components
{
    public class ComponentInitializerFactory
    {
        private readonly PythonEngineService _pythonEngineService;
        private readonly StorageService _storageService;
        private readonly RepositoryService _repositoryService;
        private readonly ILoggerFactory _loggerFactory;

        public ComponentInitializerFactory(
            PythonEngineService pythonEngineService,
            StorageService storageService,
            RepositoryService repositoryService,
            ILoggerFactory loggerFactory)
        {
            _pythonEngineService = pythonEngineService ?? throw new ArgumentNullException(nameof(pythonEngineService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public ComponentInitializer Create(ComponentRegistryService componentRegistryService)
        {
            if (componentRegistryService == null) throw new ArgumentNullException(nameof(componentRegistryService));

            return new ComponentInitializer(componentRegistryService, _pythonEngineService, _storageService, _repositoryService, _loggerFactory);
        }
    }
}
