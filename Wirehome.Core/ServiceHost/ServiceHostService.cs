using System;
using System.Collections.Generic;
using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Contracts;
using Wirehome.Core.ServiceHost.Configuration;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Model;
using Wirehome.Core.Packages;
using Wirehome.Core.Python;
using Wirehome.Core.ServiceHost.Exceptions;
using Wirehome.Core.Storage;
using Wirehome.Core.System;

namespace Wirehome.Core.ServiceHost
{
    public class ServiceHostService : IService
    {
        private const string ServicesDirectory = "Services";

        private readonly Dictionary<string, ServiceInstance> _services = new Dictionary<string, ServiceInstance>();

        private readonly PackageManagerService _repositoryService;
        private readonly StorageService _storageService;
        private readonly PythonScriptHostFactoryService _pythonScriptHostFactoryService;
        private readonly ILogger _logger;

        public ServiceHostService(
            StorageService storageService,
            PackageManagerService repositoryService,
            PythonScriptHostFactoryService pythonScriptHostFactoryService,
            SystemService systemService,
            SystemStatusService systemStatusService,
            ILogger<ServiceHostService> logger)
        {
            _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
            _pythonScriptHostFactoryService = pythonScriptHostFactoryService ?? throw new ArgumentNullException(nameof(pythonScriptHostFactoryService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (systemStatusService == null) throw new ArgumentNullException(nameof(systemStatusService));
            systemStatusService.Set("service_host.service_count", () => _services.Count);

            if (systemService == null) throw new ArgumentNullException(nameof(systemService));
            systemService.StartupCompleted += (s, e) =>
            {
                StartDelayedServices();
            };
        }

        public void Start()
        {
            foreach (var serviceUid in GetServiceUids())
            {
                TryInitializeService(serviceUid, new ServiceInitializationOptions { SkipIfDelayed = true });
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

        public void InitializeService(string id, ServiceInitializationOptions options)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (options == null) throw new ArgumentNullException(nameof(options));

            try
            {
                if (!_storageService.TryRead(out ServiceConfiguration configuration, ServicesDirectory, id, DefaultFilenames.Configuration))
                {
                    throw new ServiceNotFoundException(id);
                }

                if (!configuration.IsEnabled)
                {
                    _logger.LogInformation($"Service '{id}' not initialized because it is disabled.");
                    return;
                }

                if (configuration.DelayedStart && options.SkipIfDelayed)
                {
                    return;
                }

                if (!configuration.DelayedStart && options.SkipIfNotDelayed)
                {
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
            catch
            {
                lock (_services)
                {
                    _services.Remove(id);
                }

                throw;
            }
        }

        public void TryInitializeService(string id, ServiceInitializationOptions options)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (options == null) throw new ArgumentNullException(nameof(options));

            try
            {
                InitializeService(id, options);
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

        private void StartDelayedServices()
        {
            _logger.LogInformation("Starting delayed services.");

            foreach (var serviceUid in GetServiceUids())
            {
                TryInitializeService(serviceUid, new ServiceInitializationOptions { SkipIfNotDelayed = true });
            }
        }

        private ServiceInstance CreateServiceInstance(string id, ServiceConfiguration configuration)
        {
            var packageUid = new PackageUid(id, configuration.Version);
            var package = _repositoryService.LoadPackage(packageUid);

            var scriptHost = _pythonScriptHostFactoryService.CreateScriptHost(_logger);
            scriptHost.Compile(package.Script);

            var context = new WirehomeDictionary
            {
                ["service_id"] = id,
                [ "service_version" ] = configuration.Version
            };

            scriptHost.AddToWirehomeWrapper("context", context);

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
