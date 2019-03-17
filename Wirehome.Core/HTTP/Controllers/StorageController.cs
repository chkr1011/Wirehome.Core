using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Wirehome.Core.Storage;

namespace Wirehome.Core.HTTP.Controllers
{
    [ApiController]
    public class StorageController : Controller
    {
        private readonly StorageService _storageService;

        public StorageController(StorageService storageService)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        }

        [HttpPost]
        [Route("api/v1/settings/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostSettings([FromBody] JObject value, params string[] uid)
        {
            _storageService.Write(value, uid);
        }

        [HttpGet]
        [Route("api/v1/settings/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object GetSettings(params string[] uid)
        {
            if (!_storageService.TryRead(out JObject value, uid))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }

            return value;
        }

        [HttpDelete]
        [Route("api/v1/settings/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteSettings(params string[] uid)
        {
            _storageService.DeleteFile(uid);
        }
    }
}
