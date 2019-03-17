using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Packages;

namespace Wirehome.Core.HTTP.Controllers
{
    [ApiController]
    public class PackageManagerController : Controller
    {
        private readonly PackageManagerService _packageManagerService;

        public PackageManagerController(PackageManagerService packageManagerService)
        {
            _packageManagerService = packageManagerService ?? throw new ArgumentNullException(nameof(packageManagerService));
        }

        [HttpGet]
        [Route("/api/v1/packages/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public PackageMetaData GetMetaInformation(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var packageUid = PackageUid.Parse(uid);
            if (string.IsNullOrEmpty(packageUid.Version))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return null;
            }

            return _packageManagerService.GetMetaData(packageUid);
        }

        [HttpGet]
        [Route("/api/v1/packages/{uid}/description")]
        [ApiExplorerSettings(GroupName = "v1")]
        public string GetDescription(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var packageUid = PackageUid.Parse(uid);
            if (string.IsNullOrEmpty(packageUid.Version))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return null;
            }

            return _packageManagerService.GetDescription(packageUid);
        }

        [HttpGet]
        [Route("/api/v1/packages/{uid}/release_notes")]
        [ApiExplorerSettings(GroupName = "v1")]
        public string GetReleaseNotes(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var packageUid = PackageUid.Parse(uid);
            if (string.IsNullOrEmpty(packageUid.Version))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return null;
            }

            return _packageManagerService.GetReleaseNotes(packageUid);
        }

        [HttpPost]
        [Route("/api/v1/packages/{uid}/download")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task DownloadPackage(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var packageUid = PackageUid.Parse(uid);
            if (string.IsNullOrEmpty(packageUid.Version))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            await _packageManagerService.DownloadPackageAsync(packageUid);
        }

        [HttpPost]
        [Route("/api/v1/packages/{uid}/fork")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task ForkPackage(string uid, string forkUid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (uid == null) throw new ArgumentNullException(nameof(forkUid));

            var packageUid = PackageUid.Parse(uid);
            if (string.IsNullOrEmpty(packageUid.Version))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            var packageForkUid = PackageUid.Parse(forkUid);
            if (string.IsNullOrEmpty(packageForkUid.Version))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            await _packageManagerService.ForkPackageAsync(packageUid, packageForkUid);
        }

        [HttpDelete]
        [Route("/api/v1/packages/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeletePackage(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _packageManagerService.DeletePackage(PackageUid.Parse(uid));
        }
    }
}
