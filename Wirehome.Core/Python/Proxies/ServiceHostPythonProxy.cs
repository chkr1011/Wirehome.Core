#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using Wirehome.Core.ServiceHost;

namespace Wirehome.Core.Python.Proxies
{
    public class ServiceHostPythonProxy : IPythonProxy
    {
        private readonly ServiceHostService _serviceHostService;
        
        public ServiceHostPythonProxy(ServiceHostService serviceHostService)
        {
            _serviceHostService = serviceHostService ?? throw new ArgumentNullException(nameof(serviceHostService));
        }

        public string ModuleName { get; } = "services";

        public object invoke(string serviceId, string functionName, params object[] parameters)
        {
            if (serviceId == null) throw new ArgumentNullException(nameof(serviceId));
            if (functionName == null) throw new ArgumentNullException(nameof(functionName));

            return _serviceHostService.InvokeFunction(serviceId, functionName, parameters);
        }
    }
}
