using Microsoft.Extensions.Logging;
using System;
using Wirehome.Core.Contracts;

namespace Wirehome.Core.Python
{
    public class PythonScriptHostFactoryService : IService
    {
        private readonly PythonEngineService _pythonEngineService;
        private readonly PythonProxyFactory _pythonProxyFactory;

        public PythonScriptHostFactoryService(PythonEngineService pythonEngineService, PythonProxyFactory pythonProxyFactory)
        {
            _pythonEngineService = pythonEngineService ?? throw new ArgumentNullException(nameof(pythonEngineService));
            _pythonProxyFactory = pythonProxyFactory ?? throw new ArgumentNullException(nameof(pythonProxyFactory));
        }

        public void Start()
        {
        }

        public PythonScriptHost CreateScriptHost(ILogger logger, params IPythonProxy[] transientPythonProxies)
        {
            if (transientPythonProxies == null) throw new ArgumentNullException(nameof(transientPythonProxies));

            var pythonProxies = _pythonProxyFactory.GetPythonProxies();
            pythonProxies.AddRange(transientPythonProxies);

            return _pythonEngineService.CreateScriptHost(pythonProxies, logger);
        }
    }
}
