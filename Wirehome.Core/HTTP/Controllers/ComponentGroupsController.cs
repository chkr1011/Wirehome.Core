using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Components.Groups;

namespace Wirehome.Core.HTTP.Controllers;

[ApiController]
public sealed class ComponentGroupsController : Controller
{
    readonly ComponentGroupRegistryService _componentGroupRegistryService;

    public ComponentGroupsController(ComponentGroupRegistryService componentGroupRegistryService)
    {
        _componentGroupRegistryService = componentGroupRegistryService ?? throw new ArgumentNullException(nameof(componentGroupRegistryService));
    }

    [HttpPost]
    [Route("/api/v1/components/{componentGroupUid}/tags/{tag}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void AddTag(string componentGroupUid, string tag)
    {
        _componentGroupRegistryService.SetComponentGroupTag(componentGroupUid, tag);
    }

    // TODO: Move to dedicated factory!
    public static object CreateComponentGroupModel(ComponentGroup componentGroup)
    {
        if (componentGroup is null)
        {
            throw new ArgumentNullException(nameof(componentGroup));
        }

        return new
        {
            componentGroup.Uid,
            componentGroup.Hash,
            Status = componentGroup.GetStatus(),
            Settings = componentGroup.GetSettings(),
            Tags = componentGroup.GetTags(),
            componentGroup.Components,
            componentGroup.Macros
        };
    }

    [HttpDelete]
    [Route("api/v1/component_groups/{componentGroupUid}/components/{componentUid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void DeleteComponent(string componentGroupUid, string componentUid)
    {
        _componentGroupRegistryService.UnassignComponent(componentGroupUid, componentUid);
    }

    [HttpDelete]
    [Route("api/v1/component_groups/{componentGroupUid}/components/{componentUid}/settings/{settingUid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void DeleteComponentAssociationSetting(string componentGroupUid, string componentUid, string settingUid)
    {
        _componentGroupRegistryService.RemoveComponentAssociationSetting(componentGroupUid, componentUid, settingUid);
    }

    [HttpDelete]
    [Route("/api/v1/component_groups/{uid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void DeleteComponentGroup(string uid)
    {
        _componentGroupRegistryService.DeleteComponentGroup(uid);
    }

    [HttpDelete]
    [Route("api/v1/component_groups/{componentGroupUid}/macros/{macroUid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void DeleteMacro(string componentGroupUid, string macroUid)
    {
        _componentGroupRegistryService.UnassignMacro(componentGroupUid, macroUid);
    }

    [HttpDelete]
    [Route("/api/v1/component_groups/{componentGroupUid}/settings/{settingUid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public object DeleteSetting(string componentGroupUid, string settingUid)
    {
        return _componentGroupRegistryService.RemoveComponentGroupSetting(componentGroupUid, settingUid);
    }

    [HttpDelete]
    [Route("/api/v1/components/{componentGroupUid}/tags/{tag}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void DeleteTag(string componentGroupUid, string tag)
    {
        _componentGroupRegistryService.RemoveComponentGroupTag(componentGroupUid, tag);
    }

    [HttpGet]
    [Route("api/v1/component_groups/{componentGroupUid}/components/{componentUid}/settings/{settingUid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public object GetComponentAssociationSetting(string componentGroupUid, string componentUid, string settingUid)
    {
        return _componentGroupRegistryService.GetComponentAssociationSetting(componentGroupUid, componentUid, settingUid);
    }

    [HttpGet]
    [Route("api/v1/component_groups/{uid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public ComponentGroup GetComponentGroup(string uid)
    {
        return _componentGroupRegistryService.GetComponentGroup(uid);
    }

    [HttpGet]
    [Route("api/v1/component_groups")]
    [ApiExplorerSettings(GroupName = "v1")]
    public List<object> GetComponentGroups()
    {
        return _componentGroupRegistryService.GetComponentGroups().Select(g => CreateComponentGroupModel(g)).ToList();
    }

    [HttpGet]
    [Route("/api/v1/tags/{tag}/component_groups")]
    [ApiExplorerSettings(GroupName = "v1")]
    public List<string> GetComponentsWithTag(string tag)
    {
        var componentGroup = _componentGroupRegistryService.GetComponentGroupsWithTag(tag);
        return componentGroup.Select(c => c.Uid).ToList();
    }

    [HttpGet]
    [Route("api/v1/component_groups/{uid}/configuration")]
    [ApiExplorerSettings(GroupName = "v1")]
    public ComponentGroupConfiguration GetConfiguration(string uid)
    {
        return _componentGroupRegistryService.ReadComponentGroupConfiguration(uid);
    }

    [HttpGet]
    [Route("/api/v1/component_groups/{componentGroupUid}/settings/{settingUid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public object GetSetting(string componentGroupUid, string settingUid)
    {
        return _componentGroupRegistryService.GetComponentGroupSetting(componentGroupUid, settingUid);
    }

    [HttpGet]
    [Route("/api/v1/component_groups/{uid}/settings")]
    [ApiExplorerSettings(GroupName = "v1")]
    public IDictionary GetSettings(string uid)
    {
        return _componentGroupRegistryService.GetComponentGroup(uid).GetSettings();
    }

    [HttpGet]
    [Route("/api/v1/component_groups/{uid}/status")]
    [ApiExplorerSettings(GroupName = "v1")]
    public IDictionary GetStatus(string uid)
    {
        return _componentGroupRegistryService.GetComponentGroup(uid).GetStatus();
    }

    [HttpGet]
    [Route("/api/v1/component_groups/{componentGroupUid}/tags/{tag}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public string GetTag(string componentGroupUid, string tag)
    {
        var componentGroup = _componentGroupRegistryService.GetComponentGroup(componentGroupUid);
        if (!componentGroup.HasTag(tag))
        {
            HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return null;
        }

        return tag;
    }

    [HttpGet]
    [Route("/api/v1/component_groups/{componentGroupUid}/tags")]
    [ApiExplorerSettings(GroupName = "v1")]
    public List<string> GetTags(string componentGroupUid)
    {
        var componentGroup = _componentGroupRegistryService.GetComponentGroup(componentGroupUid);
        return componentGroup.GetTags();
    }

    [HttpPost]
    [Route("api/v1/component_groups/{componentGroupUid}/components/{componentUid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void PostComponent(string componentGroupUid, string componentUid)
    {
        _componentGroupRegistryService.AssignComponent(componentGroupUid, componentUid);
    }

    [HttpPost]
    [Route("api/v1/component_groups/{componentGroupUid}/components/{componentUid}/settings/{settingUid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void PostComponentAssociationSetting(string componentGroupUid, string componentUid, string settingUid, [FromBody] object settingValue)
    {
        _componentGroupRegistryService.SetComponentAssociationSetting(componentGroupUid, componentUid, settingUid, settingValue);
    }

    [HttpPost]
    [Route("api/v1/component_groups/{uid}/configuration")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void PostConfiguration(string uid, [FromBody] ComponentGroupConfiguration configuration)
    {
        _componentGroupRegistryService.WriteComponentGroupConfiguration(uid, configuration);
    }

    [HttpPost]
    [Route("/api/v1/component_groups/{uid}/initialize")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void PostInitialize(string uid)
    {
        _componentGroupRegistryService.InitializeComponentGroup(uid);
    }

    [HttpPost]
    [Route("api/v1/component_groups/{componentGroupUid}/macros/{macroUid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void PostMacro(string componentGroupUid, string macroUid)
    {
        _componentGroupRegistryService.AssignMacro(componentGroupUid, macroUid);
    }

    [HttpPost]
    [Route("/api/v1/component_groups/{componentGroupUid}/settings/{settingUid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void PostSetting(string componentGroupUid, string settingUid, [FromBody] object value)
    {
        _componentGroupRegistryService.SetComponentGroupSetting(componentGroupUid, settingUid, value);
    }
}