using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Backup;

namespace Wirehome.Core.HTTP.Controllers;

[ApiController]
public sealed class BackupController : Controller
{
    readonly BackupService _backupService;

    public BackupController(BackupService backupService)
    {
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
    }

    [HttpPost]
    [Route("api/v1/backups/create")]
    [ApiExplorerSettings(GroupName = "v1")]
    public Task CreateBackup()
    {
        return _backupService.CreateBackupAsync();
    }

    [HttpDelete]
    [Route("api/v1/backups/{uid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void DeleteBackup(string uid)
    {
        _backupService.DeleteBackup(uid);
    }

    [HttpGet]
    [Route("api/v1/backups/{uid}/download")]
    [ApiExplorerSettings(GroupName = "v1")]
    public IActionResult DownloadBackup(string uid)
    {
        var filename = _backupService.GetBackupFilename(uid);
        return File(filename, MediaTypeNames.Application.Zip);
    }

    [HttpGet]
    [Route("api/v1/backups/uids")]
    [ApiExplorerSettings(GroupName = "v1")]
    public List<string> GetBackupUids()
    {
        return _backupService.GetBackupUids();
    }
}