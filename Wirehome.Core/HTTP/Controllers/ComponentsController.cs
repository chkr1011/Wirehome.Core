using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Wirehome.Core.Areas;
using Wirehome.Core.Components;
using Wirehome.Core.Components.Configuration;
using Wirehome.Core.Model;

namespace Wirehome.Core.HTTP.Controllers
{
    public class ComponentsController : Controller
    {
        private readonly ComponentRegistryService _componentRegistryService;
        private readonly AreaRegistryService _areaRegistryService;

        public ComponentsController(ComponentRegistryService componentRegistryService, AreaRegistryService areaRegistryService)
        {
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
            _areaRegistryService = areaRegistryService ?? throw new ArgumentNullException(nameof(areaRegistryService));
        }

        [HttpGet]
        [Route("api/v1/components")]
        [ApiExplorerSettings(GroupName = "v1")]
        public List<Component> GetComponents(string area_uid = null)
        {
            if (area_uid == null)
            {
                return _componentRegistryService.GetComponents();
            }

            return _areaRegistryService.GetComponentsOfArea(area_uid);
        }

        [HttpPost]
        [Route("api/v1/components/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostComponent(string uid, [FromBody] ComponentConfiguration configuration)
        {
            _componentRegistryService.TryInitializeComponent(uid, configuration, out _);
        }

        [HttpPost]
        [Route("/api/v1/components/{componentUid}/execute_command")]
        [ApiExplorerSettings(GroupName = "v1")]
        public WirehomeDictionary PostComponentCommand(string componentUid, [FromBody] WirehomeDictionary message)
        {
            var oldComponent = JObject.FromObject(_componentRegistryService.GetComponent(componentUid));
            var result = _componentRegistryService.ProcessComponentMessage(componentUid, message);
            result["new_component"] = _componentRegistryService.GetComponent(componentUid);
            result["old_component"] = oldComponent;
            return result;
        }

        [HttpPost]
        [Route("/api/v1/components/{componentUid}/settings/{settingUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostComponentSetting(string componentUid, string settingUid, [FromBody] object value)
        {
            _componentRegistryService.SetComponentSetting(componentUid, settingUid, value);
        }

        [HttpGet]
        [Route("/api/v1/components/{componentUid}/settings/{settingUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object GetComponentSetting(string componentUid, string settingUid)
        {
            return _componentRegistryService.GetComponentSetting(componentUid, settingUid);
        }

        [HttpGet]
        [Route("/api/v1/components/{componentUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public Component GetComponent(string componentUid)
        {
            if (_componentRegistryService.TryGetComponent(componentUid, out var component))
            {
                return component;
            }

            // TODO: Change to 404 NotFound.
            return null;
        }

        [HttpGet]
        [Route("/api/v1/components/{componentUid}/status")]
        [ApiExplorerSettings(GroupName = "v1")]
        public WirehomeDictionary GetComponentStatus(string componentUid)
        {
            if (_componentRegistryService.TryGetComponent(componentUid, out var component))
            {
                return component.Status;
            }

            // TODO: Change to 404 NotFound.
            return null;
        }

        [HttpGet]
        [Route("/api/v1/components/{componentUid}/settings")]
        [ApiExplorerSettings(GroupName = "v1")]
        public WirehomeDictionary GetComponentSettings(string componentUid)
        {
            if (_componentRegistryService.TryGetComponent(componentUid, out var component))
            {
                return component.Settings;
            }
            
            // TODO: Change to 404 NotFound.
            return null;
        }

        [HttpGet]
        [Route("/api/v1/components/{componentUid}/configuration")]
        [ApiExplorerSettings(GroupName = "v1")]
        public WirehomeDictionary GetComponentConfiguration(string componentUid)
        {
            if (_componentRegistryService.TryGetComponent(componentUid, out var component))
            {
                return component.Configuration;
            }

            // TODO: Change to 404 NotFound.
            return null;
        }
    }
}
