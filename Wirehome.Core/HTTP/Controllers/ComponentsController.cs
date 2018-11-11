using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Components;
using Wirehome.Core.Components.Configuration;
using Wirehome.Core.Components.Exceptions;
using Wirehome.Core.Model;

namespace Wirehome.Core.HTTP.Controllers
{
    public class ComponentsController : Controller
    {
        private readonly ComponentRegistryService _componentRegistryService;

        public ComponentsController(ComponentRegistryService componentRegistryService)
        {
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
        }

        [HttpGet]
        [Route("api/v1/components/uids")]
        [ApiExplorerSettings(GroupName = "v1")]
        public List<string> GetComponentUids()
        {
            return _componentRegistryService.GetComponentUids();
        }

        [HttpGet]
        [Route("api/v1/components")]
        [ApiExplorerSettings(GroupName = "v1")]
        public List<Component> GetComponents()
        {
            return _componentRegistryService.GetComponents();
        }

        [HttpGet]
        [Route("/api/v1/components/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public Component GetComponent(string uid)
        {
            if (!_componentRegistryService.TryGetComponent(uid, out var component))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            return component;
        }

        [HttpDelete]
        [Route("/api/v1/components/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteComponent(string uid)
        {
            _componentRegistryService.DeleteComponent(uid);
        }

        [HttpGet]
        [Route("api/v1/components/{uid}/configuration")]
        [ApiExplorerSettings(GroupName = "v1")]
        public ComponentConfiguration GetConfiguration(string uid)
        {
            try
            {
                return _componentRegistryService.ReadComponentConfiguration(uid);
            }
            catch (ComponentNotFoundException)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }
        }

        [HttpPost]
        [Route("api/v1/components/{uid}/configuration")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostConfiguration(string uid, [FromBody] ComponentConfiguration configuration)
        {
            _componentRegistryService.WriteComponentConfiguration(uid, configuration);
        }

        [HttpPost]
        [Route("api/v1/components/{uid}/initialize")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostInitialize(string uid)
        {
            _componentRegistryService.TryInitializeComponent(uid);
        }

        [HttpPost]
        [Route("/api/v1/components/{uid}/process_message")]
        [ApiExplorerSettings(GroupName = "v1")]
        public WirehomeDictionary PostProcessMessage(string uid, [FromBody] WirehomeDictionary message)
        {
            var result = _componentRegistryService.ProcessComponentMessage(uid, message);

            if (!_componentRegistryService.TryGetComponent(uid, out var component))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            result["component"] = component;
            return result;
        }

        [HttpGet]
        [Route("/api/v1/components/{uid}/settings")]
        [ApiExplorerSettings(GroupName = "v1")]
        public ConcurrentWirehomeDictionary GetSettingValues(string uid)
        {
            if (!_componentRegistryService.TryGetComponent(uid, out var component))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            return component.Settings;
        }

        [HttpGet]
        [Route("/api/v1/components/{componentUid}/settings/{settingUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object GetSettingValue(string componentUid, string settingUid)
        {
            return _componentRegistryService.GetComponentSetting(componentUid, settingUid);
        }

        [HttpPost]
        [Route("/api/v1/components/{componentUid}/settings/{settingUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostSettingValue(string componentUid, string settingUid, [FromBody] object value)
        {
            _componentRegistryService.SetComponentSetting(componentUid, settingUid, value);
        }

        [HttpDelete]
        [Route("/api/v1/components/{componentUid}/settings/{settingUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object DeleteSetting(string componentUid, string settingUid)
        {
            return _componentRegistryService.RemoveComponentSetting(componentUid, settingUid);
        }

        [HttpGet]
        [Route("/api/v1/components/{uid}/status")]
        [ApiExplorerSettings(GroupName = "v1")]
        public ConcurrentWirehomeDictionary GetStatusValues(string uid)
        {
            if (!_componentRegistryService.TryGetComponent(uid, out var component))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            return component.Status;
        }

        [HttpGet]
        [Route("/api/v1/components/{componentUid}/status/{statusUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object GetStatusValue(string componentUid, string statusUid)
        {
            if (!_componentRegistryService.TryGetComponent(componentUid, out var component))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            if (!component.Status.TryGetValue(statusUid, out var value))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            return value;
        }

        [HttpPost]
        [Route("/api/v1/components/{componentUid}/status/{statusUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostStatusValue(string componentUid, string statusUid, [FromBody] object value)
        {
            if (!_componentRegistryService.TryGetComponent(componentUid, out var component))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }

            component.Status[statusUid] = value;
        }

        [HttpGet]
        [Route("/api/v1/components/{uid}/runtime_configuration")]
        [ApiExplorerSettings(GroupName = "v1")]
        public ConcurrentWirehomeDictionary GetConfigurationValues(string uid)
        {
            if (!_componentRegistryService.TryGetComponent(uid, out var component))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            return component.Configuration;
        }

        [HttpGet]
        [Route("/api/v1/components/{componentUid}/runtime_configuration/{configurationUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object GetConfigurationValue(string componentUid, string configurationUid)
        {
            if (!_componentRegistryService.TryGetComponent(componentUid, out var component))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            if (!component.Configuration.TryGetValue(configurationUid, out var value))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            return value;
        }
    }
}
