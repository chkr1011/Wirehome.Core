using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Wirehome.Core.ServiceHost;
using Wirehome.Core.ServiceHost.Configuration;
using Wirehome.Core.ServiceHost.Exceptions;

namespace Wirehome.Core.HTTP.Controllers
{
    [ApiController]
    public class ServicesController : Controller
    {
        private readonly ServiceHostService _serviceHostService;

        public ServicesController(ServiceHostService serviceHostService)
        {
            _serviceHostService = serviceHostService ?? throw new ArgumentNullException(nameof(serviceHostService));
        }

        [HttpGet]
        [Route("api/v1/services/uids")]
        public List<string> GetServiceUids()
        {
            return _serviceHostService.GetServiceUids();
        }

        [HttpGet]
        [Route("api/v1/services")]
        public List<ServiceInstance> GetServices()
        {
            return _serviceHostService.GetServices();
        }

        [HttpGet]
        [Route("api/v1/services/{id}")]
        public ServiceInstance GetService(string id)
        {
            var service = _serviceHostService.GetServices().FirstOrDefault(s => s.Id == id);
            if (service == null)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }

            return service;
        }

        [HttpDelete]
        [Route("api/v1/services/{id}")]
        public void DeleteService(string id)
        {
            _serviceHostService.DeleteService(id);
        }

        [HttpGet]
        [Route("api/v1/services/{id}/configuration")]
        public ServiceConfiguration GetConfiguration(string id)
        {
            try
            {
                return _serviceHostService.ReadServiceConfiguration(id);
            }
            catch (ServiceNotFoundException)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }
        }

        [HttpPost]
        [Route("api/v1/services/{id}/configuration")]
        public void PostConfiguration(string id, [FromBody] ServiceConfiguration serviceConfiguration)
        {
            _serviceHostService.WriteServiceConfiguration(id, serviceConfiguration);
        }

        [HttpPost]
        [Route("/api/v1/services/{id}/initialize")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostInitialize(string id)
        {
            _serviceHostService.InitializeService(id);
        }

        [HttpPost]
        [Route("/api/v1/services/{id}/invoke_function")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object PostInvokeFunction(string id, string functionName, [FromBody] object[] parameters = null)
        {
            return _serviceHostService.InvokeFunction(id, functionName, parameters);
        }

        [HttpGet]
        [Route("/api/v1/services/{id}/status")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object GetStatus(string id)
        {
            return _serviceHostService.InvokeFunction(id, "get_service_status");
        }
    }
}
