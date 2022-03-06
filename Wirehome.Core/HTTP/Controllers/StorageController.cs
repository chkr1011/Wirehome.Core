using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Wirehome.Core.Storage;

namespace Wirehome.Core.HTTP.Controllers;

[ApiController]
public sealed class StorageController : Controller
{
    readonly StorageService _storageService;

    public StorageController(StorageService storageService)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
    }

    [HttpDelete]
    [Route("api/v1/settings/{*uid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void DeleteSettings(string uid)
    {
        if (uid is null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        _storageService.DeletePath(uid.Split("/"));
    }

    [HttpGet]
    [Route("api/v1/settings/{*uid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public object GetSettings(string uid)
    {
        if (uid is null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        if (!_storageService.TryReadSerializedValue(out JObject value, uid.Split("/")))
        {
            HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
        }

        return value;
    }

    [HttpPost]
    [Route("api/v1/settings/{*uid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void PostSetting([FromBody] JObject value, string uid)
    {
        if (uid is null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        _storageService.WriteSerializedValue(value, uid.Split("/"));
    }
}