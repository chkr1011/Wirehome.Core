using System;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Storage;

namespace Wirehome.Core.HTTP.Controllers
{
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
        public void PostSettings([FromBody] object value, params string[] path)
        {
            _storageService.Write(value, path);
        }

        //[HttpGet]
        //[Route("api/v1/settings/{uid}")]
        //[ApiExplorerSettings(GroupName = "v1")]
        //public object GetSettings(params string[] path)
        //{
        //    _storageService.TryRead()
        //}

        //[HttpDelete]
        //[Route("api/v1/settings/{uid}")]
        //[ApiExplorerSettings(GroupName = "v1")]
        //public void DeleteSettings(params string[] path)
        //{
        //    _storageService.Delete(path);
        //}
    }
}
