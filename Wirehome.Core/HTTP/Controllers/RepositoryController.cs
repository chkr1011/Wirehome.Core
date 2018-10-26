using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Repository;

namespace Wirehome.Core.HTTP.Controllers
{
    public class RepositoryController : Controller
    {
        private readonly RepositoryService _repositoryService;

        public RepositoryController(RepositoryService repositoryService)
        {
            _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
        }

        [HttpGet]
        [Route("/api/v1/repository/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public RepositoryEntityMetaData GetMetaInformation(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var entityUid = RepositoryEntityUid.Parse(uid);
            if (string.IsNullOrEmpty(entityUid.Version))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return null;
            }

            return _repositoryService.GetMetaData(entityUid);
        }

        [HttpGet]
        [Route("/api/v1/repository/{uid}/description")]
        [ApiExplorerSettings(GroupName = "v1")]
        public string GetDescription(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var entityUid = RepositoryEntityUid.Parse(uid);
            if (string.IsNullOrEmpty(entityUid.Version))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return null;
            }

            return _repositoryService.GetDescription(entityUid);
        }

        [HttpGet]
        [Route("/api/v1/repository/{uid}/release_notes")]
        [ApiExplorerSettings(GroupName = "v1")]
        public string GetReleaseNotes(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var entityUid = RepositoryEntityUid.Parse(uid);
            if (string.IsNullOrEmpty(entityUid.Version))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return null;
            }

            return _repositoryService.GetReleaseNotes(entityUid);
        }

        [HttpPost]
        [Route("/api/v1/repository/{uid}/download")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task DownloadEntity(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var entityUid = RepositoryEntityUid.Parse(uid);
            if (string.IsNullOrEmpty(entityUid.Version))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            await _repositoryService.DownloadEntityAsync(entityUid);
        }

        [HttpDelete]
        [Route("/api/v1/repository/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteEntity(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _repositoryService.DeleteEntity(RepositoryEntityUid.Parse(uid));
        }
    }
}
