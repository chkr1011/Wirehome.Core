using System;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Python;
using Wirehome.Core.Repository;

namespace Wirehome.Core.ServiceHost
{
    public class ServiceInstance
    {
        private readonly object _syncRoot = new object();

        private readonly RepositoryService _repositoryService;
        private readonly PythonEngineService _pythonEngineService;
        private readonly ILogger _logger;

        private PythonScriptHost _pythonScriptHost;

        public RepositoryEntityUid RepositoryEntityUid { get; private set; }

        public ServiceInstance(RepositoryService repositoryService, PythonEngineService pythonEngineService, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
            _pythonEngineService = pythonEngineService ?? throw new ArgumentNullException(nameof(pythonEngineService));
            
            _logger = loggerFactory.CreateLogger<ServiceInstance>();
        }

        public void Initialize()
        {
            Initialize(RepositoryEntityUid);
        }

        public void Initialize(RepositoryEntityUid repositoryEntityUid)
        {
            RepositoryEntityUid = repositoryEntityUid ?? throw new ArgumentNullException(nameof(repositoryEntityUid));

            var repositoryEntitySource = _repositoryService.LoadEntity(RepositoryEntityUid);

            lock (_syncRoot)
            {
                _pythonScriptHost = _pythonEngineService.CreateScriptHost(_logger);
                _pythonScriptHost.Initialize(repositoryEntitySource.Script);
            }
        }

        public void SetVariable(string key, object value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            lock (_syncRoot)
            {
                _pythonScriptHost.SetVariable(key, value);
            }
        }

        public object ExecuteFunction(string name, params object[] parameters)
        {
            lock (_syncRoot)
            {
                return _pythonScriptHost.InvokeFunction(name, parameters);
            }
        }
    }
}
