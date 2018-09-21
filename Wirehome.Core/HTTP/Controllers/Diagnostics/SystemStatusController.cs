using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Diagnostics;

namespace Wirehome.Core.HTTP.Controllers.Diagnostics
{
    public class SystemStatusController : Controller
    {
        private readonly SystemStatusService _systemStatusService;

        public SystemStatusController(SystemStatusService systemStatusService)
        {
            _systemStatusService = systemStatusService ?? throw new ArgumentNullException(nameof(systemStatusService));
        }

        [HttpGet]
        [Route("/api/v1/system_status")]
        [ApiExplorerSettings(GroupName = "v1")]
        public Dictionary<string, object> GetSystemStatus()
        {
            return _systemStatusService.All();
        }

        [HttpGet]
        [Route("/api/v1/system_status/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object GetSystemStatus(string uid)
        {
            return _systemStatusService.Get(uid);
        }

        [HttpPost]
        [Route("/api/v1/system_status/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostSystemStatus(string uid, [FromBody] object value)
        {
            _systemStatusService.Set(uid, value);
        }
    }
}