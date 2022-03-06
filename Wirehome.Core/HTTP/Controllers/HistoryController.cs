using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Components;
using Wirehome.Core.Components.History;
using Wirehome.Core.History;
using Wirehome.Core.History.Extract;
using Wirehome.Core.History.Repository;

namespace Wirehome.Core.HTTP.Controllers;

[ApiController]
public sealed class HistoryController : Controller
{
    readonly ComponentHistoryService _componentHistoryService;
    readonly ComponentRegistryService _componentRegistryService;
    readonly HistoryService _historyService;

    public HistoryController(ComponentHistoryService componentHistoryService, HistoryService historyService, ComponentRegistryService componentRegistryService)
    {
        _componentHistoryService = componentHistoryService ?? throw new ArgumentNullException(nameof(componentHistoryService));
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
    }

    [HttpDelete]
    [Route("api/v1/components/{componentUid}/history")]
    [ApiExplorerSettings(GroupName = "v1")]
    public Task DeleteComponentHistory(string componentUid)
    {
        var path = _componentHistoryService.BuildComponentHistoryPath(componentUid);

        return _historyService.DeleteHistory(path, HttpContext.RequestAborted);
    }

    [HttpDelete]
    [Route("api/v1/components/{componentUid}/status/{statusUid}/history")]
    [ApiExplorerSettings(GroupName = "v1")]
    public Task DeleteComponentStatusHistory(string componentUid, string statusUid)
    {
        var path = _componentHistoryService.BuildComponentStatusHistoryPath(componentUid, statusUid);

        return _historyService.DeleteHistory(path, HttpContext.RequestAborted);
    }

    [HttpDelete]
    [Route("api/v1/history")]
    [ApiExplorerSettings(GroupName = "v1")]
    public async Task DeleteEntireHistory()
    {
        foreach (var component in _componentRegistryService.GetComponents())
        {
            await _historyService.DeleteHistory(_componentHistoryService.BuildComponentHistoryPath(component.Uid), HttpContext.RequestAborted).ConfigureAwait(false);
        }
    }

    [HttpDelete]
    [Route("api/v1/history/statistics")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void DeleteStatistics()
    {
        _historyService.ResetStatistics();
    }

    [HttpGet]
    [Route("api/v1/components/{componentUid}/history/size")]
    [ApiExplorerSettings(GroupName = "v1")]
    public Task<long> GetComponentHistorySize(string componentUid)
    {
        var path = _componentHistoryService.BuildComponentHistoryPath(componentUid);

        return _historyService.GetHistorySize(path, HttpContext.RequestAborted);
    }

    [HttpGet]
    [Route("api/v1/components/{componentUid}/status/{statusUid}/history")]
    [ApiExplorerSettings(GroupName = "v1")]
    public async Task<ActionResult<HistoryExtract>> GetComponentStatusHistory(string componentUid,
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

        var path = _componentHistoryService.BuildComponentStatusHistoryPath(componentUid, statusUid);

        var historyExtract = await _historyService.BuildHistoryExtractAsync(
            path, rangeStart.Value.UtcDateTime, rangeEnd.Value.UtcDateTime, interval, dataType.Value, maxRowCount, HttpContext.RequestAborted).ConfigureAwait(false);

        return new ObjectResult(historyExtract);
    }

    [HttpGet]
    [Route("api/v1/components/{componentUid}/status/{statusUid}/history/raw")]
    [ApiExplorerSettings(GroupName = "v1")]
    public async Task<ActionResult<HistoryExtract>> GetComponentStatusHistoryRaw(string componentUid, string statusUid, int year, int month, int day)
    {
        var readOperation = new HistoryReadOperation
        {
            Path = _componentHistoryService.BuildComponentStatusHistoryPath(componentUid, statusUid),
            RangeStart = new DateTime(year, month, day, 0, 0, 0),
            RangeEnd = new DateTime(year, month, day, 23, 59, 59),
            MaxEntityCount = null
        };

        var result = await _historyService.Read(readOperation, HttpContext.RequestAborted).ConfigureAwait(false);

        return new ObjectResult(result);
    }

    [HttpGet]
    [Route("api/v1/components/{componentUid}/status/{statusUid}/history/size")]
    [ApiExplorerSettings(GroupName = "v1")]
    public Task<long> GetComponentStatusHistorySize(string componentUid, string statusUid)
    {
        var path = _componentHistoryService.BuildComponentStatusHistoryPath(componentUid, statusUid);

        return _historyService.GetHistorySize(path, HttpContext.RequestAborted);
    }
}