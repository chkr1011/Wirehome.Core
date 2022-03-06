using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.GlobalVariables;

namespace Wirehome.Core.HTTP.Controllers;

[ApiController]
public sealed class GlobalVariablesController : Controller
{
    readonly GlobalVariablesService _globalVariablesService;

    public GlobalVariablesController(GlobalVariablesService globalVariablesService)
    {
        _globalVariablesService = globalVariablesService ?? throw new ArgumentNullException(nameof(globalVariablesService));
    }

    [HttpDelete]
    [Route("api/v1/global_variables/{uid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void DeleteGlobalVariable(string uid)
    {
        _globalVariablesService.DeleteValue(uid);
    }

    [HttpGet]
    [Route("api/v1/global_variables/{uid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public object GetGlobalVariable(string uid)
    {
        if (!_globalVariablesService.ValueExists(uid))
        {
            HttpContext.Response.StatusCode = (int)HttpStatusCode.NoContent;
            return null;
        }

        return _globalVariablesService.GetValue(uid);
    }

    [HttpGet]
    [Route("api/v1/global_variables")]
    [ApiExplorerSettings(GroupName = "v1")]
    public IDictionary<string, object> GetGlobalVariables()
    {
        return _globalVariablesService.GetValues();
    }

    [HttpPost]
    [Route("api/v1/global_variables/{uid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void PostGlobalVariable(string uid, [FromBody] object value)
    {
        _globalVariablesService.SetValue(uid, value);
    }
}