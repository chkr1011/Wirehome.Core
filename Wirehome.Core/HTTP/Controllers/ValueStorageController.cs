using Microsoft.AspNetCore.Mvc;
using System;
using Wirehome.Core.Storage;

namespace Wirehome.Core.HTTP.Controllers
{
    [ApiController]
    public class ValueStorageController : Controller
    {
        private readonly ValueStorageService _valueStorageService;

        public ValueStorageController(ValueStorageService valueStorageService)
        {
            _valueStorageService = valueStorageService ?? throw new ArgumentNullException(nameof(valueStorageService));
        }

        [HttpGet]
        [Route("api/v1/value_storage/{container}/{key}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object GetValueStorageValue(string container, string key)
        {
            if (_valueStorageService.TryRead<object>(container, key, out var value))
            {
                return value;
            }

            return NotFound();
        }

        [HttpPost]
        [Route("api/v1/value_storage/{container}/{key}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostValueStorageValue(string container, string key, [FromBody] object value)
        {
            _valueStorageService.Write(container, key, value);
        }

        [HttpDelete]
        [Route("api/v1/value_storage/{container}/{key}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteValueStorageValue(string container, string key)
        {
            _valueStorageService.Delete(container, key);
        }
    }
}
