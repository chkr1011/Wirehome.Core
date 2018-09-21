using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Repositories;

namespace Wirehome.Core.HTTP.Controllers
{
    public class RepositoriesController : Controller
    {
        private readonly RepositoryService _repositoryService;

        public RepositoriesController(RepositoryService repositoryService)
        {
            _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
        }

        [HttpPost]
        [Route("/api/v1/repositories/{type}/download")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task DownloadEntity(string type, string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var typeBuffer = (RepositoryType)Enum.Parse(typeof(RepositoryType), type, true);
            await _repositoryService.DownloadEntityAsync(typeBuffer, RepositoryEntityUid.Parse(uid));
        }

        [HttpDelete]
        [Route("/api/v1/repositories/{type}/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteEntity(string type, string uid)
        {
            var typeBuffer = (RepositoryType)Enum.Parse(typeof(RepositoryType), type, true);
            _repositoryService.DeleteEntity(typeBuffer, RepositoryEntityUid.Parse(uid));
        }
    }
}
