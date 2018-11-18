using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Wirehome.Core.Contracts;

namespace Wirehome.Core.Python
{
    public class PythonProxyFactory : IService
    {
        private readonly IServiceProvider _serviceProvider;
        
        public PythonProxyFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public void Start()
        {
        }

        public List<IPythonProxy> GetPythonProxies()
        {
            return _serviceProvider.GetServices<IPythonProxy>().ToList();
        }
    }
}
