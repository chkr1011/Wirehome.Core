using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Diagnostics.Log;

namespace Wirehome.Core.HTTP.Controllers;

[ApiController]
public sealed class LogController : Controller
{
    readonly LogService _logService;

    public LogController(LogService logService)
    {
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
    }

    [HttpDelete]
    [Route("/api/v1/log")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void DeleteLog()
    {
        _logService.Clear();
    }

    [HttpDelete]
    [Route("/api/v1/log/settings/publish_to_mqtt")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void DeletePublishToMqtt()
    {
        _logService.Sender.PublishToMqtt = false;
    }

    [HttpDelete]
    [Route("/api/v1/log/settings/udp_receiver")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void DeletePublishToUdp()
    {
        _logService.Sender.UdpReceiverEndPoint = null;
    }

    [HttpGet]
    [Route("/api/v1/log")]
    [ApiExplorerSettings(GroupName = "v1")]
    public List<LogEntry> GetLog(bool includeInformation = false, bool includeWarnings = false, bool includeErrors = false, int takeCount = 1000)
    {
        var filter = new LogEntryFilter
        {
            IncludeInformation = includeInformation,
            IncludeWarnings = includeWarnings,
            IncludeErrors = includeErrors,
            TakeCount = takeCount
        };

        return _logService.GetEntries(filter);
    }

    [HttpPost]
    [Route("/api/v1/log/settings/publish_to_mqtt")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void PostPublishToMqtt()
    {
        _logService.Sender.PublishToMqtt = true;
    }

    [HttpPost]
    [Route("/api/v1/log/settings/udp_receiver")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void PostPublishToUdp(string ipAddress, int port)
    {
        _logService.Sender.UdpReceiverEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
    }
}