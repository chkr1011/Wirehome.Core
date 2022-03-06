using System;
using System.Collections.Generic;
using System.Linq;
using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Packages;
using Wirehome.Core.Python;
using Wirehome.Core.ServiceHost.Configuration;
using Wirehome.Core.ServiceHost.Exceptions;
using Wirehome.Core.Storage;
using Wirehome.Core.System;

namespace Wirehome.Core.ServiceHost;

public sealed class ServiceHostService : WirehomeCoreService
{
    const string ServicesDirectory = "Services";
    readonly ILogger _logger;
    readonly PythonScriptHostFactoryService _pythonScriptHostFactoryService;

    readonly PackageManagerService _repositoryService;

    readonly Dictionary<string, ServiceInstance> _services = new();
    readonly StorageService _storageService;

    public ServiceHostService(StorageService storageService,
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

        if (systemStatusService == null)
        {
            throw new ArgumentNullException(nameof(systemStatusService));
        }

        systemStatusService.Set("service_host.service_count", () => _services.Count);

        if (systemService == null)
        {
            throw new ArgumentNullException(nameof(systemService));
        }

        systemService.StartupCompleted += (s, e) => { StartDelayedServices(); };
    }

    public void DeleteService(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        _storageService.DeletePath(ServicesDirectory, id);
    }

    public List<ServiceInstance> GetServices()
    {
        lock (_services)
        {
            return new List<ServiceInstance>(_services.Values);
        }
    }

    public List<string> GetServiceUids()
    {
        return _storageService.EnumerateDirectories("*", ServicesDirectory);
    }

    public void InitializeService(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        try
        {
            if (!_storageService.TryReadSerializedValue(out ServiceConfiguration configuration, ServicesDirectory, id, DefaultFileNames.Configuration))
            {
                throw new ServiceNotFoundException(id, null);
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
        catch
        {
            lock (_services)
            {
                _services.Remove(id);
            }

            throw;
        }
    }

    public object InvokeFunction(string serviceId, string functionName, params object[] parameters)
    {
        if (serviceId == null)
        {
            throw new ArgumentNullException(nameof(serviceId));
        }

        if (functionName == null)
        {
            throw new ArgumentNullException(nameof(functionName));
        }

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

    public ServiceConfiguration ReadServiceConfiguration(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        if (!_storageService.TryReadSerializedValue(out ServiceConfiguration configuration, ServicesDirectory, id, DefaultFileNames.Configuration))
        {
            throw new ServiceNotFoundException(id, null);
        }

        return configuration;
    }

    public void TryInitializeService(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        try
        {
            InitializeService(id);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Error while initializing service '{id}'.");
        }
    }

    public void WriteServiceConfiguration(string id, ServiceConfiguration configuration)
    {
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _storageService.WriteSerializedValue(configuration, ServicesDirectory, id, DefaultFileNames.Configuration);
    }

    protected override void OnStart()
    {
        foreach (var serviceUid in ReadServiceConfigurations().Where(i => !i.Value.DelayedStart).Select(i => i.Key))
        {
            TryInitializeService(serviceUid);
        }
    }

    ServiceInstance CreateServiceInstance(string id, ServiceConfiguration configuration)
    {
        var packageUid = new PackageUid(id, configuration.Version);
        var package = _repositoryService.LoadPackage(packageUid);

        var scriptHost = _pythonScriptHostFactoryService.CreateScriptHost();
        scriptHost.Compile(package.Script);

        var context = new PythonDictionary
        {
            ["service_id"] = id,
            ["service_version"] = configuration.Version,
            ["service_uid"] = new PackageUid(id, configuration.Version).ToString()
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

    Dictionary<string, ServiceConfiguration> ReadServiceConfigurations()
    {
        var serviceConfigurations = new Dictionary<string, ServiceConfiguration>();
        foreach (var serviceUid in GetServiceUids())
        {
            if (_storageService.TryReadSerializedValue(out ServiceConfiguration serviceConfiguration, ServicesDirectory, serviceUid, DefaultFileNames.Configuration))
            {
                serviceConfigurations.Add(serviceUid, serviceConfiguration);
            }
        }

        return serviceConfigurations;
    }

    void StartDelayedServices()
    {
        _logger.LogInformation("Starting delayed services.");

        foreach (var serviceUid in ReadServiceConfigurations().Where(i => i.Value.DelayedStart).Select(i => i.Key))
        {
            TryInitializeService(serviceUid);
        }
    }
}