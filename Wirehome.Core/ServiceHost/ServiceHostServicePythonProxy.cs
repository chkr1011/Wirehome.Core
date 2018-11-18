#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using Wirehome.Core.Python;

namespace Wirehome.Core.ServiceHost
{
    public class ServiceHostServicePythonProxy : IInjectedPythonProxy
    {
        private readonly ServiceHostService _serviceHostService;
        
        public ServiceHostServicePythonProxy(ServiceHostService serviceHostService)
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
