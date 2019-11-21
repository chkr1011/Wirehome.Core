using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using Wirehome.Core.Backup;

namespace Wirehome.Core.HTTP.Controllers
{
    [ApiController]
    public class BackupController : Controller
    {
        private readonly BackupService _backupService;

        public BackupController(BackupService backupService)
        {
            _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        }

        [HttpGet]
        [Route("api/v1/backups/uids")]
        [ApiExplorerSettings(GroupName = "v1")]
        public List<string> GetBackupUids()
        {
            return _backupService.GetBackupUids();
        }

        [HttpDelete]
        [Route("api/v1/backups/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteBackup(string uid)
        {
            _backupService.DeleteBackup(uid);
        }

        [HttpPost]
        [Route("api/v1/backups/create")]
        [ApiExplorerSettings(GroupName = "v1")]
        public Task PostBackup()
        {
            return _backupService.CreateBackupAsync();
        }

        [HttpGet]
        [Route("api/v1/backups/{uid}/download")]
        [ApiExplorerSettings(GroupName = "v1")]
        public IActionResult DownloadBackup(string uid)
        {
            return File(global::System.IO.File.OpenRead(_backupService.GetBackupFilename(uid)), MediaTypeNames.Application.Zip);
        }
    }
}
