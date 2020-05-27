using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Wirehome.Core.Python
{
    public sealed class PythonProxyFactory
    {
        readonly IServiceProvider _serviceProvider;

        List<IPythonProxy> _pythonProxies;

        public PythonProxyFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public void PreparePythonProxies()
        {
            _pythonProxies = _serviceProvider.GetServices<IPythonProxy>().ToList();
        }

        public List<IPythonProxy> GetPythonProxies()
        {
            // Create a copy of the list that the callers can modify it without danger!
            return new List<IPythonProxy>(_pythonProxies);
        }
    }
}
