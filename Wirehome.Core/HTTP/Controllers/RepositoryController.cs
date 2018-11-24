using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Repository;

namespace Wirehome.Core.HTTP.Controllers
{
    public class PackageRegistryController : Controller
    {
        private readonly PackageRegistryService _packageRegistryService;

        public PackageRegistryController(PackageRegistryService packageRegistryService)
        {
            _packageRegistryService = packageRegistryService ?? throw new ArgumentNullException(nameof(packageRegistryService));
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

            return _packageRegistryService.GetMetaData(packageUid);
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

            return _packageRegistryService.GetDescription(packageUid);
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

            return _packageRegistryService.GetReleaseNotes(packageUid);
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

            await _packageRegistryService.DownloadPackageAsync(packageUid);
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

            await _packageRegistryService.ForkPackageAsync(packageUid, packageForkUid);
        }

        [HttpDelete]
        [Route("/api/v1/packages/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeletePackage(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _packageRegistryService.DeletePackage(PackageUid.Parse(uid));
        }
    }
}
