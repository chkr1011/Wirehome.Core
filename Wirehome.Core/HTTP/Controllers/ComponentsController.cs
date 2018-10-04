using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Wirehome.Core.Components;
using Wirehome.Core.Components.Configuration;
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
        [Route("api/v1/components")]
        [ApiExplorerSettings(GroupName = "v1")]
        public List<Component> Get()
        {
            return _componentRegistryService.GetComponents();
        }

        [HttpPost]
        [Route("api/v1/components/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void Post(string uid, [FromBody] ComponentConfiguration configuration)
        {
            _componentRegistryService.TryInitializeComponent(uid, configuration, out _);
        }

        [HttpPost]
        [Route("/api/v1/components/{uid}/execute_command")]
        [ApiExplorerSettings(GroupName = "v1")]
        public WirehomeDictionary PostComponentCommand(string uid, [FromBody] WirehomeDictionary message)
        {
            var oldComponent = JObject.FromObject(_componentRegistryService.GetComponent(uid));
            var result = _componentRegistryService.ProcessComponentMessage(uid, message);
            result["new_component"] = _componentRegistryService.GetComponent(uid);
            result["old_component"] = oldComponent;
            return result;
        }

        [HttpPost]
        [Route("/api/v1/components/{componentUid}/settings/{settingUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostSetting(string componentUid, string settingUid, [FromBody] object value)
        {
            _componentRegistryService.SetComponentSetting(componentUid, settingUid, value);
        }

        [HttpGet]
        [Route("/api/v1/components/{componentUid}/settings/{settingUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object GetSetting(string componentUid, string settingUid)
        {
            return _componentRegistryService.GetComponentSetting(componentUid, settingUid);
        }

        [HttpDelete]
        [Route("/api/v1/components/{componentUid}/settings/{settingUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object DeleteSetting(string componentUid, string settingUid)
        {
            return _componentRegistryService.RemoveComponentSetting(componentUid, settingUid);
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

        [HttpGet]
        [Route("/api/v1/components/{uid}/status")]
        [ApiExplorerSettings(GroupName = "v1")]
        public WirehomeDictionary GetComponentStatus(string uid)
        {
            if (!_componentRegistryService.TryGetComponent(uid, out var component))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            return component.Status;
        }

        [HttpGet]
        [Route("/api/v1/components/{uid}/settings")]
        [ApiExplorerSettings(GroupName = "v1")]
        public WirehomeDictionary GetComponentSettings(string uid)
        {
            if (!_componentRegistryService.TryGetComponent(uid, out var component))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            return component.Settings;
        }

        [HttpGet]
        [Route("/api/v1/components/{uid}/configuration")]
        [ApiExplorerSettings(GroupName = "v1")]
        public WirehomeDictionary GetComponentConfiguration(string uid)
        {
            if (!_componentRegistryService.TryGetComponent(uid, out var component))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            return component.Configuration;
        }
    }
}
