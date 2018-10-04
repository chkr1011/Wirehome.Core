using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.ServiceHost;
using Wirehome.Core.ServiceHost.Configuration;

namespace Wirehome.Core.HTTP.Controllers
{
    public class ServicesController : Controller
    {
        private readonly ServiceHostService _serviceHostService;

        public ServicesController(ServiceHostService serviceHostService)
        {
            _serviceHostService = serviceHostService ?? throw new ArgumentNullException(nameof(serviceHostService));
        }

        [HttpPost]
        [Route("/api/v1/services/{id}/initialize")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostInitialize(string id, string version, [FromBody] Dictionary<string, object> variables)
        {
            var configuration = new ServiceConfiguration
            {
                Version = version,
                Variables = variables
            };

            _serviceHostService.TryInitializeService(id, configuration);
        }

        [HttpPost]
        [Route("/api/services/{id}/invoke_function")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object PostInvokeFunction(string id, string functionName, [FromBody] object[] parameters)
        {
            return _serviceHostService.InvokeFunction(id, functionName, parameters);
        }

        [HttpGet]
        [Route("/api/services/{id}/status")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object GetStatus(string id)
        {
            return _serviceHostService.InvokeFunction(id, "get_status");
        }
    }
}
