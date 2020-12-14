using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Wirehome.Core.Packages;

namespace Wirehome.Core.HTTP.Controllers
{
    [ApiController]
    public class PackageManagerController : Controller
    {
        readonly PackageManagerService _packageManagerService;

        public PackageManagerController(PackageManagerService packageManagerService)
        {
            _packageManagerService = packageManagerService ?? throw new ArgumentNullException(nameof(packageManagerService));
        }

        [HttpGet]
        [Route("/api/v1/packages/index/local")]
        [ApiExplorerSettings(GroupName = "v1")]
        public Dictionary<string, HashSet<string>> GetLocalPackageIndex()
        {
            var packageUids = _packageManagerService.GetPackageUids();
            return GeneratePackageIndex(packageUids);
        }

        [HttpGet]
        [Route("/api/v1/packages/index/remote")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task<Dictionary<string, HashSet<string>>> GetRemotePackageIndex()
        {
            var packageUids = await _packageManagerService.FetchRemotePackageUidsAsync().ConfigureAwait(false);
            return GeneratePackageIndex(packageUids);
        }

        [HttpGet]
        [Route("/api/v1/packages/uids")]
        [ApiExplorerSettings(GroupName = "v1")]
        public List<PackageUid> GetPackageUids()
        {
            return _packageManagerService.GetPackageUids();
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

            return _packageManagerService.GetPackageMetaData(packageUid);
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

            return _packageManagerService.GetPackageDescription(packageUid);
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

            return _packageManagerService.GetPackageReleaseNotes(packageUid);
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

            await _packageManagerService.DownloadPackageAsync(packageUid).ConfigureAwait(false);
        }

        [HttpPost]
        [Route("/api/v1/packages/{uid}/fork")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task ForkPackage(string uid, string forkUid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (forkUid == null) throw new ArgumentNullException(nameof(forkUid));

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

            await _packageManagerService.ForkPackageAsync(packageUid, packageForkUid).ConfigureAwait(false);
        }

        [HttpDelete]
        [Route("/api/v1/packages/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeletePackage(string uid)
        {
            _packageManagerService.DeletePackage(PackageUid.Parse(uid));
        }

        static Dictionary<string, HashSet<string>> GeneratePackageIndex(List<PackageUid> packageUids)
        {
            var index = new Dictionary<string, HashSet<string>>();

            foreach (var packageUid in packageUids)
            {
                if (!index.ContainsKey(packageUid.Id))
                {
                    index.Add(packageUid.Id, new HashSet<string>());
                }

                index[packageUid.Id].Add(packageUid.Version);
            }

            return index;
        }
    }
}
