using System;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Storage;

namespace Wirehome.Core.HTTP.Controllers;

[ApiController]
public sealed class ValueStorageController : Controller
{
    readonly ValueStorageService _valueStorageService;

    public ValueStorageController(ValueStorageService valueStorageService)
    {
        _valueStorageService = valueStorageService ?? throw new ArgumentNullException(nameof(valueStorageService));
    }

    [HttpDelete]
    [Route("api/v1/value_storage/{*path}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void DeleteValueStorageValue(string path)
    {
        _valueStorageService.Delete(RelativeValueStoragePath.Parse(path));
    }

    [HttpGet]
    [Route("api/v1/value_storage/{*path}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public object GetValueStorageValue(string path)
    {
        if (_valueStorageService.TryRead<object>(RelativeValueStoragePath.Parse(path), out var value))
        {
            return value;
        }

        return NotFound();
    }

    [HttpPost]
    [Route("api/v1/value_storage/{*path}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void PostValueStorageValue(string path, [FromBody] object value)
    {
        _valueStorageService.Write(RelativeValueStoragePath.Parse(path), value);
    }
}