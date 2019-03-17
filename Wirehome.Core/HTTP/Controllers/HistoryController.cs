using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.History;
using Wirehome.Core.History.Extract;

namespace Wirehome.Core.HTTP.Controllers
{
    [ApiController]
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
        public async Task<ActionResult<HistoryExtract>> GetComponentStatusHistoryAsync(
            string componentUid, 
            string statusUid,
            DateTimeOffset? rangeStart,
            DateTimeOffset? rangeEnd,
            TimeSpan? interval,
            HistoryExtractDataType? dataType,
            int maxRowCount = 1000)
        {
            if (rangeEnd == null)
            {
                rangeEnd = DateTimeOffset.UtcNow;
            }

            if (rangeStart == null)
            {
                rangeStart = rangeEnd.Value.AddHours(-1);
            }

            if (dataType == null)
            {
                dataType = HistoryExtractDataType.Text;
            }

            if (dataType == HistoryExtractDataType.Number && interval == null)
            {
                interval = TimeSpan.FromMinutes(5);
            }

            if (dataType == HistoryExtractDataType.Text && interval != null)
            {
                return new ObjectResult(new ProblemDetails
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "Parameters are invalid.",
                    Detail = "Interval is not supported for data type TEXT."
                });
            }

            var historyExtract = await _historyService.BuildHistoryExtractAsync(
                componentUid,
                statusUid,
                rangeStart.Value.UtcDateTime,
                rangeEnd.Value.UtcDateTime,
                interval,
                dataType.Value,
                maxRowCount,
                HttpContext.RequestAborted);

            return new ObjectResult(historyExtract);
        }

        [HttpDelete]
        [Route("api/v1/history/components/{componentUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public Task DeleteComponentHistoryAsync(string componentUid, DateTime? rangeStart, DateTime? rangeEnd)
        {
            return _historyService.DeleteComponentStatusHistoryAsync(componentUid, null, rangeStart, rangeEnd, HttpContext.RequestAborted);
        }

        [HttpDelete]
        [Route("api/v1/history/components/{componentUid}/status/{statusUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public Task DeleteComponentStatusHistoryAsync(string componentUid, string statusUid, DateTime? rangeStart, DateTime? rangeEnd)
        {
            return _historyService.DeleteComponentStatusHistoryAsync(componentUid, statusUid, rangeStart, rangeEnd, HttpContext.RequestAborted);
        }

        [HttpDelete]
        [Route("api/v1/history/status/{statusUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public Task DeleteStatusHistoryAsync(string statusUid, DateTime? rangeStart, DateTime? rangeEnd)
        {
            return _historyService.DeleteComponentStatusHistoryAsync(null, statusUid, rangeStart, rangeEnd, HttpContext.RequestAborted);
        }

        [HttpGet]
        [Route("api/v1/history/components/{componentUid}/row_count")]
        [ApiExplorerSettings(GroupName = "v1")]
        public Task<int> GetRowCountForComponentHistoryAsync(string componentUid, DateTime? rangeStart, DateTime? rangeEnd)
        {
            return _historyService.GetRowCountForComponentStatusHistoryAsync(componentUid, null, rangeStart, rangeEnd, HttpContext.RequestAborted);
        }

        [HttpGet]
        [Route("api/v1/history/components/{componentUid}/status/{statusUid}/row_count")]
        [ApiExplorerSettings(GroupName = "v1")]
        public Task<int> GetRowCountForComponentStatusHistoryAsync(string componentUid, string statusUid, DateTime? rangeStart, DateTime? rangeEnd)
        {
            return _historyService.GetRowCountForComponentStatusHistoryAsync(componentUid, statusUid, rangeStart, rangeEnd, HttpContext.RequestAborted);
        }

        [HttpGet]
        [Route("api/v1/history/status/{statusUid}/row_count")]
        [ApiExplorerSettings(GroupName = "v1")]
        public Task<int> GetRowCountForStatusHistoryAsync(string statusUid, DateTime? rangeStart, DateTime? rangeEnd)
        {
            return _historyService.GetRowCountForComponentStatusHistoryAsync(null, statusUid, rangeStart, rangeEnd, HttpContext.RequestAborted);
        }
    }
}
