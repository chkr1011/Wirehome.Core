using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Wirehome.Core.ServiceHost.Configuration;
using Wirehome.Core.Areas;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Exceptions;
using Wirehome.Core.Python.Proxies;
using Wirehome.Core.Repositories;
using Wirehome.Core.Storage;

namespace Wirehome.Core.ServiceHost
{
    public class ServiceHostService
    {
        private readonly Dictionary<string, ServiceInstance> _serviceInstances = new Dictionary<string, ServiceInstance>();

        private readonly RepositoryService _repositoryService;
        private readonly ILoggerFactory _loggerFactory;
        private readonly StorageService _storageService;
        private readonly PythonEngineService _pythonEngineService;
        private readonly ILogger _logger;

        public ServiceHostService(
            StorageService storageService,
            RepositoryService repositoryService,
            PythonEngineService pythonEngineService,
            SystemStatusService systemStatusService,
            ILoggerFactory loggerFactory)
        {
            if (systemStatusService == null) throw new ArgumentNullException(nameof(systemStatusService));
            _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
            _pythonEngineService = pythonEngineService ?? throw new ArgumentNullException(nameof(pythonEngineService));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        
            _logger = loggerFactory.CreateLogger<AreaRegistryService>();

            pythonEngineService.RegisterSingletonProxy(new ServiceHostPythonProxy(this));

            systemStatusService.Set("running_services", () => _serviceInstances.Count);
        }

        public void Start()
        {
            if (_storageService.TryRead(out List<ServiceConfiguration> configurations, "Services.json"))
            {
                foreach (var configuration in configurations)
                {
                    TryInitializeService(configuration);
                }
            }
        }

        public void TryInitializeService(ServiceConfiguration configuration)
        {
            if (!configuration.IsEnabled)
            {
                _logger.Log(LogLevel.Information, $"Service '{configuration.Uid}' not initialized because it is disabled.");
                return;
            }

            try
            {
                var serviceInstance = new ServiceInstance(_repositoryService, _pythonEngineService, _loggerFactory);
                serviceInstance.Initialize(configuration.Uid);

                if (configuration.Variables != null)
                {
                    foreach (var variable in configuration.Variables)
                    {
                        serviceInstance.SetVariable(variable.Key, variable.Value);
                    }
                }

                _logger.Log(LogLevel.Information, "Initializing service '{0}'.", configuration.Uid);
                serviceInstance.ExecuteFunction("initialize");

                lock (_serviceInstances)
                {
                    if (_serviceInstances.TryGetValue(configuration.Uid.Id, out var existingServiceInstance))
                    {
                        _logger.Log(LogLevel.Information, "Stopping service '{0}'.", configuration.Uid);
                        existingServiceInstance.ExecuteFunction("stop");
                    }

                    _serviceInstances[configuration.Uid.Id] = serviceInstance;

                    _logger.Log(LogLevel.Information, "Starting service '{0}'.", configuration.Uid);
                    serviceInstance.ExecuteFunction("start");
                }
                
                _logger.Log(LogLevel.Information, $"Service '{configuration.Uid}' started.");
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, exception, $"Error while initializing service '{configuration.Uid}'.");
            }
        }

        public object InvokeFunction(string serviceId, string functionName, params object[] parameters)
        {
            ServiceInstance serviceInstance;
            lock (_serviceInstances)
            {
                if (!_serviceInstances.TryGetValue(serviceId, out serviceInstance))
                {
                    throw new PythonProxyException($"Service '{serviceId}' unknown or not started.");
                }
            }

            return serviceInstance.ExecuteFunction(functionName, parameters);
        }
    }
}
