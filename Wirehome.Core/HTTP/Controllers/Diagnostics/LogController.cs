using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Diagnostics.Log;

namespace Wirehome.Core.HTTP.Controllers.Diagnostics
{
    public class LogController : Controller
    {
        private readonly LogService _logService;

        public LogController(LogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        [HttpGet]
        [Route("/api/v1/log")]
        [ApiExplorerSettings(GroupName = "v1")]
        public List<LogEntry> GetLog(bool includeInformations = false, bool includeWarnings = false, bool includeErrors = false)
        {
            return _logService.GetEntries(new LogEntryFilter
            {
                IncludeInformations = includeInformations,
                IncludeWarnings = includeWarnings,
                IncludeErrors = includeErrors
            });
        }

        [HttpDelete]
        [Route("/api/v1/log")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteLog()
        {
            _logService.Clear();
        }
    }
}
