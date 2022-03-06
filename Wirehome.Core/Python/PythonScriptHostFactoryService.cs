using System;
using Wirehome.Core.Contracts;

namespace Wirehome.Core.Python;

public sealed class PythonScriptHostFactoryService : WirehomeCoreService
{
    readonly PythonEngineService _pythonEngineService;
    readonly PythonProxyFactory _pythonProxyFactory;

    public PythonScriptHostFactoryService(PythonEngineService pythonEngineService, PythonProxyFactory pythonProxyFactory)
    {
        _pythonEngineService = pythonEngineService ?? throw new ArgumentNullException(nameof(pythonEngineService));
        _pythonProxyFactory = pythonProxyFactory ?? throw new ArgumentNullException(nameof(pythonProxyFactory));
    }

    public PythonScriptHost CreateScriptHost(params IPythonProxy[] transientPythonProxies)
    {
        var pythonProxies = _pythonProxyFactory.GetPythonProxies();

        if (transientPythonProxies != null)
        {
            pythonProxies.AddRange(transientPythonProxies);
        }

        return _pythonEngineService.CreateScriptHost(pythonProxies);
    }
}