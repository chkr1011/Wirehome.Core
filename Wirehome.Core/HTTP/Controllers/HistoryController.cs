using System;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.History;
using Wirehome.Core.History.Extract;

namespace Wirehome.Core.HTTP.Controllers
{
    public class HistoryController : Controller
    {
        private readonly HistoryService _historyService;

        public HistoryController(HistoryService historyService)
        {
            _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        }

        [HttpGet]
        [Route("api/v1/history/{componentUid}/{statusUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public HistoryExtract GetHistoryExtract(string componentUid, string statusUid, DateTimeOffset? rangeStart, DateTimeOffset? rangeEnd, TimeSpan? interval, HistoryExtractDataType? dataType)
        {
            if (rangeEnd == null)
            {
                rangeEnd = DateTimeOffset.UtcNow;
            }

            if (rangeStart == null)
            {
                rangeStart = rangeEnd.Value.AddHours(-24);
            }

            if (interval == null)
            {
                interval = TimeSpan.FromMinutes(5);
            }

            if (dataType == null)
            {
                dataType = HistoryExtractDataType.Text;
            }

            return _historyService.BuildHistoryExtract(componentUid, statusUid, rangeStart.Value.UtcDateTime, rangeEnd.Value.UtcDateTime, interval.Value, dataType.Value);
        }
    }
}
