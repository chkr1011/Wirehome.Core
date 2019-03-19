using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Components;
using Wirehome.Core.Model;

namespace Wirehome.Core.HTTP.Controllers
{
    [ApiController]
    public class ComponentGroupsController : Controller
    {
        private readonly ComponentGroupRegistryService _componentGroupRegistryService;

        public ComponentGroupsController(ComponentGroupRegistryService componentGroupRegistryService)
        {
            _componentGroupRegistryService = componentGroupRegistryService ?? throw new ArgumentNullException(nameof(componentGroupRegistryService));
        }

        [HttpGet]
        [Route("api/v1/component_groups")]
        [ApiExplorerSettings(GroupName = "v1")]
        public List<ComponentGroup> GetComponentGroups()
        {
            return _componentGroupRegistryService.GetComponentGroups();
        }

        [HttpGet]
        [Route("api/v1/component_groups/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public ComponentGroup GetComponentGroup(string uid)
        {
            return _componentGroupRegistryService.GetComponentGroup(uid);
        }

        [HttpPost]
        [Route("api/v1/component_groups/{uid}/configuration")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostConfiguration(string uid, [FromBody] ComponentGroupConfiguration configuration)
        {
            _componentGroupRegistryService.WriteComponentGroupConfiguration(uid, configuration);
        }

        [HttpGet]
        [Route("api/v1/component_groups/{uid}/configuration")]
        [ApiExplorerSettings(GroupName = "v1")]
        public ComponentGroupConfiguration GetConfiguration(string uid)
        {
            return _componentGroupRegistryService.ReadComponentGroupConfiguration(uid);
        }

        [HttpDelete]
        [Route("/api/v1/component_groups/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteComponentGroup(string uid)
        {
            _componentGroupRegistryService.DeleteComponentGroup(uid);
        }

        [HttpPost]
        [Route("api/v1/component_groups/{componentGroupUid}/components/{componentUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostComponent(string componentGroupUid, string componentUid)
        {
            _componentGroupRegistryService.AssignComponent(componentGroupUid, componentUid);
        }

        [HttpDelete]
        [Route("api/v1/component_groups/{componentGroupUid}/components/{componentUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteComponent(string componentGroupUid, string componentUid)
        {
            _componentGroupRegistryService.UnassignComponent(componentGroupUid, componentUid);
        }

        [HttpGet]
        [Route("api/v1/component_groups/{componentGroupUid}/components/{componentUid}/settings/{settingUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object GetComponentAssociationSetting(string componentGroupUid, string componentUid, string settingUid)
        {
            return _componentGroupRegistryService.GetComponentAssociationSetting(componentGroupUid, componentUid, settingUid);
        }

        [HttpPost]
        [Route("api/v1/component_groups/{componentGroupUid}/components/{componentUid}/settings/{settingUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostComponentAssociationSetting(string componentGroupUid, string componentUid, string settingUid, [FromBody] object settingValue)
        {
            _componentGroupRegistryService.SetComponentAssociationSetting(componentGroupUid, componentUid, settingUid, settingValue);
        }

        [HttpDelete]
        [Route("api/v1/component_groups/{componentGroupUid}/components/{componentUid}/settings/{settingUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteComponentAssociationSetting(string componentGroupUid, string componentUid, string settingUid)
        {
            _componentGroupRegistryService.RemoveComponentAssociationSetting(componentGroupUid, componentUid, settingUid);
        }

        [HttpPost]
        [Route("api/v1/component_groups/{componentGroupUid}/macros/{macroUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostMacro(string componentGroupUid, string macroUid)
        {
            _componentGroupRegistryService.AssignMacro(componentGroupUid, macroUid);
        }

        [HttpDelete]
        [Route("api/v1/component_groups/{componentGroupUid}/macros/{macroUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteMacro(string componentGroupUid, string macroUid)
        {
            _componentGroupRegistryService.UnassignMacro(componentGroupUid, macroUid);
        }

        [HttpGet]
        [Route("/api/v1/component_groups/{uid}/status")]
        [ApiExplorerSettings(GroupName = "v1")]
        public ConcurrentWirehomeDictionary GetStatus(string uid)
        {
            return _componentGroupRegistryService.GetComponentGroup(uid).Status;
        }

        [HttpGet]
        [Route("/api/v1/component_groups/{uid}/settings")]
        [ApiExplorerSettings(GroupName = "v1")]
        public ConcurrentWirehomeDictionary GetSettings(string uid)
        {
            return _componentGroupRegistryService.GetComponentGroup(uid).Settings;
        }

        [HttpPost]
        [Route("/api/v1/component_groups/{componentGroupUid}/settings/{settingUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostSetting(string componentGroupUid, string settingUid, [FromBody] object value)
        {
            _componentGroupRegistryService.SetComponentGroupSetting(componentGroupUid, settingUid, value);
        }

        [HttpGet]
        [Route("/api/v1/component_groups/{componentGroupUid}/settings/{settingUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object GetSetting(string componentGroupUid, string settingUid)
        {
            return _componentGroupRegistryService.GetComponentGroupSetting(componentGroupUid, settingUid);
        }

        [HttpDelete]
        [Route("/api/v1/component_groups/{componentGroupUid}/settings/{settingUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object DeleteSetting(string componentGroupUid, string settingUid)
        {
            return _componentGroupRegistryService.RemoveComponentGroupSetting(componentGroupUid, settingUid);
        }

        [HttpPost]
        [Route("/api/v1/component_groups/{uid}/initialize")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostInitialize(string uid)
        {
            _componentGroupRegistryService.InitializeComponentGroup(uid);
        }
    }
}
