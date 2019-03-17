using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.System;

namespace Wirehome.Core.HTTP.Controllers.Diagnostics
{
    [ApiController]
    public class SystemStatusController : Controller
    {
        private readonly SystemStatusService _systemStatusService;
        private readonly SystemService _systemService;

        public SystemStatusController(SystemStatusService systemStatusService, SystemService systemService)
        {
            _systemStatusService = systemStatusService ?? throw new ArgumentNullException(nameof(systemStatusService));
            _systemService = systemService ?? throw new ArgumentNullException(nameof(systemService));
        }

        [HttpGet]
        [Route("/api/v1/system/ping")]
        [ApiExplorerSettings(GroupName = "v1")]
        public string GetPing()
        {
            return "pong";
        }

        [HttpGet]
        [Route("/api/v1/system/status")]
        [ApiExplorerSettings(GroupName = "v1")]
        public Dictionary<string, object> GetSystemStatus()
        {
            return _systemStatusService.All();
        }

        [HttpGet]
        [Route("/api/v1/system/status/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object GetSystemStatus(string uid)
        {
            return _systemStatusService.Get(uid);
        }

        [HttpPost]
        [Route("/api/v1/system/status/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostSystemStatus(string uid, [FromBody] object value)
        {
            _systemStatusService.Set(uid, value);
        }

        [HttpPost]
        [Route("/api/v1/system/reboot")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostReboot()
        {
            _systemService.Reboot(5);
        }
    }
}