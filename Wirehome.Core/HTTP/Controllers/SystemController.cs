using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Packages;
using Wirehome.Core.Storage;
using Wirehome.Core.System;

namespace Wirehome.Core.HTTP.Controllers;

[ApiController]
public sealed class SystemController : Controller
{
    readonly PackageManagerService _packageManagerService;
    readonly StorageService _storageService;
    readonly SystemService _systemService;
    readonly SystemStatusService _systemStatusService;

    public SystemController(SystemStatusService systemStatusService, SystemService systemService, PackageManagerService packageManagerService, StorageService storageService)
    {
        _systemStatusService = systemStatusService ?? throw new ArgumentNullException(nameof(systemStatusService));
        _systemService = systemService ?? throw new ArgumentNullException(nameof(systemService));
        _packageManagerService = packageManagerService ?? throw new ArgumentNullException(nameof(packageManagerService));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
    }

    [HttpGet]
    [Route("/api/v1/system/paths/bin")]
    [ApiExplorerSettings(GroupName = "v1")]
    public string GetBinPath()
    {
        return _storageService.BinPath;
    }

    [HttpGet]
    [Route("/api/v1/system/paths/data")]
    [ApiExplorerSettings(GroupName = "v1")]
    public string GetDataPath()
    {
        return _storageService.DataPath;
    }

    [HttpGet]
    [Route("/api/v1/system/ping")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void GetPing()
    {
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

    [HttpGet]
    [Route("/api/v1/system/version")]
    [ApiExplorerSettings(GroupName = "v1")]
    public string GetVersion()
    {
        return WirehomeCoreVersion.Version;
    }

    [HttpPost]
    [Route("/api/v1/system/reboot")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void PostReboot()
    {
        _systemService.Reboot(5);
    }

    [HttpPost]
    [Route("/api/v1/system/run_garbage_collector")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void PostRunGarbageCollector()
    {
        _systemService.RunGarbageCollector();
    }

    [HttpPost]
    [Route("/api/v1/system/setup")]
    [ApiExplorerSettings(GroupName = "v1")]
    public async Task PostSetup(string appPackageUid = "wirehome.app@1.0.0", string configuratorPackageUid = "wirehome.configurator@1.0.0")
    {
        if (!string.IsNullOrWhiteSpace(appPackageUid?.Trim()))
        {
            await _packageManagerService.DownloadPackageAsync(PackageUid.Parse(appPackageUid)).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(configuratorPackageUid?.Trim()))
        {
            await _packageManagerService.DownloadPackageAsync(PackageUid.Parse(configuratorPackageUid)).ConfigureAwait(false);
        }
    }

    [HttpPost]
    [Route("/api/v1/system/status/{uid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void PostSystemStatus(string uid, [FromBody] object value)
    {
        _systemStatusService.Set(uid, value);
    }
}