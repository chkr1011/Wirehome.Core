using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Wirehome.Core.ServiceHost.Configuration;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Exceptions;
using Wirehome.Core.Python.Proxies;
using Wirehome.Core.Repository;
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

            _logger = loggerFactory.CreateLogger<ServiceHostService>();

            pythonEngineService.RegisterSingletonProxy(new ServiceHostPythonProxy(this));

            systemStatusService.Set("service_host.service_count", () => _serviceInstances.Count);
        }

        public void Start()
        {
            var configurationFiles = _storageService.EnumerateFiles("Configuration.json", "Services");
            foreach (var configurationFile in configurationFiles)
            {
                if (_storageService.TryRead(out ServiceConfiguration configuration, "Services", configurationFile))
                {
                    var id = Path.GetDirectoryName(configurationFile);
                    TryInitializeService(id, configuration);
                }
            }
        }

        public void TryInitializeService(string id, ServiceConfiguration configuration)
        {
            if (!configuration.IsEnabled)
            {
                _logger.Log(LogLevel.Information, $"Service '{id}' not initialized because it is disabled.");
                return;
            }

            try
            {
                var repositoryEntityUid = new RepositoryEntityUid { Id = id, Version = configuration.Version };

                var serviceInstance = new ServiceInstance(_repositoryService, _pythonEngineService, _loggerFactory);
                serviceInstance.Initialize(repositoryEntityUid);

                if (configuration.Variables != null)
                {
                    foreach (var variable in configuration.Variables)
                    {
                        serviceInstance.SetVariable(variable.Key, variable.Value);
                    }
                }

                _logger.Log(LogLevel.Information, "Initializing service '{0}'.", id);
                serviceInstance.ExecuteFunction("initialize");

                lock (_serviceInstances)
                {
                    if (_serviceInstances.TryGetValue(id, out var existingServiceInstance))
                    {
                        _logger.Log(LogLevel.Information, "Stopping service '{0}'.", id);
                        existingServiceInstance.ExecuteFunction("stop");
                    }

                    _serviceInstances[id] = serviceInstance;

                    _logger.Log(LogLevel.Information, "Starting service '{0}'.", id);
                    serviceInstance.ExecuteFunction("start");
                }

                _logger.Log(LogLevel.Information, $"Service '{id}' started.");
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, exception, $"Error while initializing service '{id}'.");
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
