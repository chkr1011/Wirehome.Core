using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Wirehome.Core.ServiceHost.Configuration;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Proxies;
using Wirehome.Core.Repository;
using Wirehome.Core.ServiceHost.Exceptions;
using Wirehome.Core.Storage;

namespace Wirehome.Core.ServiceHost
{
    public class ServiceHostService
    {
        private const string ServicesDirectory = "Services";

        private readonly Dictionary<string, ServiceInstance> _services = new Dictionary<string, ServiceInstance>();

        private readonly RepositoryService _repositoryService;
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
            _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
            _pythonEngineService = pythonEngineService ?? throw new ArgumentNullException(nameof(pythonEngineService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<ServiceHostService>();

            pythonEngineService.RegisterSingletonProxy(new ServiceHostPythonProxy(this));

            if (systemStatusService == null) throw new ArgumentNullException(nameof(systemStatusService));
            systemStatusService.Set("service_host.service_count", () => _services.Count);
        }

        public void Start()
        {
            foreach (var serviceUid in GetServiceUids())
            {
                TryInitializeService(serviceUid);
            }
        }

        public List<string> GetServiceUids()
        {
            return _storageService.EnumeratureDirectories("*", ServicesDirectory);
        }

        public void WriteServiceConfiguration(string id, ServiceConfiguration configuration)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            _storageService.Write(configuration, ServicesDirectory, id, DefaultFilenames.Configuration);
        }

        public ServiceConfiguration ReadServiceConfiguration(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            if (!_storageService.TryRead(out ServiceConfiguration configuration, ServicesDirectory, id, DefaultFilenames.Configuration))
            {
                throw new ServiceNotFoundException(id);
            }

            return configuration;
        }

        public void DeleteService(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            
            _storageService.DeleteDirectory(ServicesDirectory, id);
        }

        public List<ServiceInstance> GetServices()
        {
            lock (_services)
            {
                return new List<ServiceInstance>(_services.Values);
            }
        }

        public void TryInitializeService(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            
            try
            {
                if (!_storageService.TryRead(out ServiceConfiguration configuration, ServicesDirectory, id, DefaultFilenames.Configuration))
                {
                    return;
                }

                if (!configuration.IsEnabled)
                {
                    _logger.LogInformation($"Service '{id}' not initialized because it is disabled.");
                    return;
                }

                _logger.LogInformation($"Initializing service '{id}'.");
                var serviceInstance = CreateServiceInstance(id, configuration);
                serviceInstance.ExecuteFunction("initialize");
                _logger.LogInformation($"Service '{id}' initialized.");

                lock (_services)
                {
                    if (_services.TryGetValue(id, out var existingServiceInstance))
                    {
                        _logger.LogInformation($"Stopping service '{id}'.");
                        existingServiceInstance.ExecuteFunction("stop");
                        _logger.LogInformation($"Service '{id}' stopped.");
                    }

                    _services[id] = serviceInstance;

                    _logger.LogInformation($"Starting service '{id}'.");
                    serviceInstance.ExecuteFunction("start");
                    _logger.LogInformation($"Service '{id}' started.");
                }               
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while initializing service '{id}'.");
            }
        }

        public object InvokeFunction(string serviceId, string functionName, params object[] parameters)
        {
            if (serviceId == null) throw new ArgumentNullException(nameof(serviceId));
            if (functionName == null) throw new ArgumentNullException(nameof(functionName));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            ServiceInstance serviceInstance;
            lock (_services)
            {
                if (!_services.TryGetValue(serviceId, out serviceInstance))
                {
                    throw new ServiceNotStartedException(serviceId);
                }
            }

            return serviceInstance.ExecuteFunction(functionName, parameters);
        }

        private ServiceInstance CreateServiceInstance(string id, ServiceConfiguration configuration)
        {
            var repositoryEntityUid = new RepositoryEntityUid(id, configuration.Version);
            var repositoryEntitySource = _repositoryService.LoadEntity(repositoryEntityUid);

            var scriptHost = _pythonEngineService.CreateScriptHost(_logger);
            scriptHost.Initialize(repositoryEntitySource.Script);

            var serviceInstance = new ServiceInstance(id, configuration, scriptHost);

            if (configuration.Variables != null)
            {
                foreach (var variable in configuration.Variables)
                {
                    serviceInstance.SetVariable(variable.Key, variable.Value);
                }
            }

            return serviceInstance;
        }
    }
}
