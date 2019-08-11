using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Packages;
using Wirehome.Core.Storage;
using Wirehome.Core.System;

namespace Wirehome.Core.HTTP.Controllers
{
    [ApiController]
    public class SystemController : Controller
    {
        private readonly SystemStatusService _systemStatusService;
        private readonly SystemService _systemService;
        private readonly PackageManagerService _packageManagerService;
        private readonly StorageService _storageService;

        public SystemController(SystemStatusService systemStatusService, SystemService systemService, PackageManagerService packageManagerService, StorageService storageService)
        {
            _systemStatusService = systemStatusService ?? throw new ArgumentNullException(nameof(systemStatusService));
            _systemService = systemService ?? throw new ArgumentNullException(nameof(systemService));
            _packageManagerService = packageManagerService ?? throw new ArgumentNullException(nameof(packageManagerService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
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

        [HttpPost]
        [Route("/api/v1/system/setup")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task PostSetup(
            string appPackageUid = "wirehome.app@1.0.0",
            string configuratorPackageUid = "wirehome.configurator@1.0.0", 
            bool fixStartupScripts = true)
        {
            if (!string.IsNullOrWhiteSpace(appPackageUid.Trim()))
            {
                await _packageManagerService.DownloadPackageAsync(PackageUid.Parse(appPackageUid));
            }

            if (!string.IsNullOrWhiteSpace(configuratorPackageUid.Trim()))
            {
                await _packageManagerService.DownloadPackageAsync(PackageUid.Parse(configuratorPackageUid));
            }
            
            if (fixStartupScripts)
            {
                FixStartupScripts();
            }
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
        [Route("/api/v1/system/version")]
        [ApiExplorerSettings(GroupName = "v1")]
        public string GetVersion()
        {
            return WirehomeCoreVersion.Version;
        }

        private void FixStartupScripts()
        {
            var path = _storageService.BinPath;

            FixFile(Path.Combine(path, "run.sh"));
            FixFile(Path.Combine(path, "rund.sh"));
        }

        private static void FixFile(string filename)
        {
            if (!global::System.IO.File.Exists(filename))
            {
                return;
            }

            var content = global::System.IO.File.ReadAllText(filename, Encoding.UTF8);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                content = content.Replace("\r\n", "\n");
            }

            global::System.IO.File.WriteAllText(filename, content, Encoding.UTF8);
        }
    }
}