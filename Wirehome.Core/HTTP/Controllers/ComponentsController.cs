using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Components;
using Wirehome.Core.Components.Configuration;
using Wirehome.Core.Components.Exceptions;
using Wirehome.Core.Constants;
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
            return _componentRegistryService.GetComponent(uid);
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
            _componentRegistryService.InitializeComponent(uid);
        }

        [HttpPost]
        [Route("api/v1/components/{uid}/enable")]
        [ApiExplorerSettings(GroupName = "v1")]
        public WirehomeDictionary PostEnable(string uid)
        {
            return _componentRegistryService.EnableComponent(uid);
        }

        [HttpPost]
        [Route("api/v1/components/{uid}/disable")]
        [ApiExplorerSettings(GroupName = "v1")]
        public WirehomeDictionary PostDisable(string uid)
        {
            return _componentRegistryService.DisableComponent(uid);
        }

        [HttpPost]
        [Route("/api/v1/components/{uid}/process_message")]
        [ApiExplorerSettings(GroupName = "v1")]
        public WirehomeDictionary PostProcessMessage(string uid, [FromBody] WirehomeDictionary message)
        {
            var result = _componentRegistryService.ProcessComponentMessage(uid, message);
            result["component"] = _componentRegistryService.GetComponent(uid);
            return result;
        }

        [HttpGet]
        [Route("/api/v1/components/{uid}/settings")]
        [ApiExplorerSettings(GroupName = "v1")]
        public ConcurrentWirehomeDictionary GetSettingValues(string uid)
        {
            return _componentRegistryService.GetComponent(uid).Settings;
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
            return _componentRegistryService.GetComponent(uid).Status;
        }


        [HttpGet]
        [Route("/api/v1/components/{uid}/debug_information")]
        [ApiExplorerSettings(GroupName = "v1")]
        public WirehomeDictionary GetDebugInformation(string uid, [FromBody] WirehomeDictionary parameters)
        {
            return _componentRegistryService.GetComponent(uid).GetDebugInformation(parameters);
        }

        [HttpGet]
        [Route("/api/v1/components/{componentUid}/status/{statusUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object GetStatusValue(string componentUid, string statusUid)
        {
            var component = _componentRegistryService.GetComponent(componentUid);

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
            _componentRegistryService.SetComponentStatus(componentUid, statusUid, value);
        }

        [HttpGet]
        [Route("/api/v1/components/{uid}/runtime_configuration")]
        [ApiExplorerSettings(GroupName = "v1")]
        public ConcurrentWirehomeDictionary GetConfigurationValues(string uid)
        {
            return _componentRegistryService.GetComponent(uid).Configuration;
        }

        [HttpGet]
        [Route("/api/v1/components/{componentUid}/runtime_configuration/{configurationUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object GetConfigurationValue(string componentUid, string configurationUid)
        {
            var component = _componentRegistryService.GetComponent(componentUid);

            if (!component.Configuration.TryGetValue(configurationUid, out var value))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            return value;
        }

        [HttpGet]
        [Route("/api/v1/components/{componentUid}/tags")]
        [ApiExplorerSettings(GroupName = "v1")]
        public List<string> GetTags(string componentUid)
        {
            var component = _componentRegistryService.GetComponent(componentUid);
            return component.Tags.ToList();
        }

        [HttpGet]
        [Route("/api/v1/components/{componentUid}/tags/{tag}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public string GetTag(string componentUid, string tag)
        {
            var component = _componentRegistryService.GetComponent(componentUid);
            if (!component.Tags.Contains(tag))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            return tag;
        }

        [HttpGet]
        [Route("/api/v1/tags/{tag}/components")]
        [ApiExplorerSettings(GroupName = "v1")]
        public List<string> GetComponentsWithTag(string componentUid, string tag)
        {
            var components = _componentRegistryService.GetComponentsWithTag(tag);
            return components.Select(c => c.Uid).ToList();
        }

        [HttpPost]
        [Route("/api/v1/components/{componentUid}/tags/{tag}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void AddTag(string componentUid, string tag)
        {
            var component = _componentRegistryService.GetComponent(componentUid);
            component.Tags.Add(tag);
        }

        [HttpDelete]
        [Route("/api/v1/components/{componentUid}/tags/{tag}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteTag(string componentUid, string tag)
        {
            var component = _componentRegistryService.GetComponent(componentUid);
            component.Tags.Remove(tag);
        }
    }
}
