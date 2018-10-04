using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.System.StartupScripts;

namespace Wirehome.Core.HTTP.Controllers
{
    public class StartupScriptsController : Controller
    {
        private readonly StartupScriptsService _startupScriptsService;

        public StartupScriptsController(StartupScriptsService startupScriptsService)
        {
            _startupScriptsService = startupScriptsService ?? throw new ArgumentNullException(nameof(startupScriptsService));
        }

        [HttpGet]
        [Route("api/v1/startup_scripts")]
        [ApiExplorerSettings(GroupName = "v1")]
        public Dictionary<string, StartupScript> Get()
        {
            return _startupScriptsService.GetStartupScripts();
        }

        [HttpGet]
        [Route("api/v1/startup_scripts/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public StartupScript Get(string uid)
        {
            return _startupScriptsService.GetStartupScript(uid);
        }

        [HttpPost]
        [Route("api/v1/startup_scripts/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void Post(string uid, [FromBody] StartupScriptConfiguration configuration)
        {
            _startupScriptsService.CreateStartupScript(uid, configuration);
        }

        [HttpDelete]
        [Route("api/v1/startup_scripts/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void Delete(string uid)
        {
            _startupScriptsService.RemoveStartupScript(uid);
        }

        [HttpGet]
        [Route("api/v1/startup_scripts/{uid}/code")]
        [ApiExplorerSettings(GroupName = "v1")]
        public string GetCode(string uid)
        {
            return _startupScriptsService.GetStartupScriptCode(uid);
        }

        [HttpPost]
        [Route("api/v1/startup_scripts/{uid}/code")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostCode(string uid)
        {
            var scriptCode = new StreamReader(HttpContext.Request.Body).ReadToEnd();
            _startupScriptsService.SetStartupScriptCode(uid, scriptCode);
        }
    }
}
