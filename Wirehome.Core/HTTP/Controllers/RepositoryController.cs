using System;
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
        public async Task GetMetaInformation(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            throw new NotImplementedException();
        }

        [HttpGet]
        [Route("/api/v1/repository/{uid}/description")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task GetDescription(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            throw new NotImplementedException();
        }

        [HttpGet]
        [Route("/api/v1/repository/{uid}/release_notes")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task GetReleaseNotes(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            throw new NotImplementedException();
        }

        [HttpPost]
        [Route("/api/v1/repository/{uid}/download")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task DownloadEntity(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            await _repositoryService.DownloadEntityAsync(RepositoryEntityUid.Parse(uid));
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
