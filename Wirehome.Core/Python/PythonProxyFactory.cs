using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Wirehome.Core.Python;

public sealed class PythonProxyFactory
{
    readonly IServiceProvider _serviceProvider;

    List<IPythonProxy> _pythonProxies;

    public PythonProxyFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public List<IPythonProxy> GetPythonProxies()
    {
        // Create a copy of the list that the callers can modify it without danger!
        return new List<IPythonProxy>(_pythonProxies);
    }

    public void PreparePythonProxies()
    {
        _pythonProxies = _serviceProvider.GetServices<IPythonProxy>().ToList();
    }
}