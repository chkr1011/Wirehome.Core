#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using System.Runtime.InteropServices;
using Wirehome.Core.Python;

namespace Wirehome.Core.ServiceHost
{
    public sealed class ServiceHostServicePythonProxy : IInjectedPythonProxy
    {
        readonly ServiceHostService _serviceHostService;

        public ServiceHostServicePythonProxy(ServiceHostService serviceHostService)
        {
            _serviceHostService = serviceHostService ?? throw new ArgumentNullException(nameof(serviceHostService));
        }

        public string ModuleName { get; } = "services";

        public object invoke(string service_id, string function_name, [DefaultParameterValue(null)] params object[] parameters)
        {
            return PythonConvert.ToPython(_serviceHostService.InvokeFunction(service_id, function_name, parameters));
        }
    }
}
