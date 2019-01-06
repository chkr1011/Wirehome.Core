using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.App;
using Wirehome.Core.Storage;

namespace Wirehome.Core.HTTP.Controllers
{
    public class AppController : Controller
    {
        private readonly AppService _appService;
        private readonly StorageService _storageService;

        public AppController(AppService appService, StorageService storageService)
        {
            _appService = appService ?? throw new ArgumentNullException(nameof(appService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        }

        [HttpGet]
        [Route("api/v1/app/version")]
        [ApiExplorerSettings(GroupName = "v1")]
        public ActionResult<string> GetVersion()
        {
            if (_storageService.TryReadBinText(out var version, "WebApp", "version.txt"))
            {
                return Content(version);
            }

            return NotFound();
        }

        [HttpGet]
        [Route("api/v1/app/panels")]
        [ApiExplorerSettings(GroupName = "v1")]
        public List<AppPanelDefinition> GetRegisteredPanels()
        {
            return _appService.GetRegisteredPanels();
        }
    }
}
