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
        [Route("/api/v1/repositories/{type}/{uid}/download")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task DownloadEntity(string type, string uid)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var typeBuffer = (RepositoryType)Enum.Parse(typeof(RepositoryType), type, true);
            await _repositoryService.DownloadEntityAsync(typeBuffer, RepositoryEntityUid.Parse(uid));
        }

        [HttpDelete]
        [Route("/api/v1/repositories/{type}/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteEntity(string type, string uid)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var typeBuffer = (RepositoryType)Enum.Parse(typeof(RepositoryType), type, true);
            _repositoryService.DeleteEntity(typeBuffer, RepositoryEntityUid.Parse(uid));
        }
        
        [HttpPost]
        [Route("/api/v1/repositories/component_logics/{uid}/download")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task DownloadComponentLogicEntity(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            await _repositoryService.DownloadEntityAsync(RepositoryType.ComponentLogics, RepositoryEntityUid.Parse(uid));
        }

        [HttpPost]
        [Route("/api/v1/repositories/component_adapters/{uid}/download")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task DownloadComponentAdapterEntity(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            await _repositoryService.DownloadEntityAsync(RepositoryType.ComponentAdapters, RepositoryEntityUid.Parse(uid));
        }

        [HttpPost]
        [Route("/api/v1/repositories/services/{uid}/download")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task DownloadServiceEntity(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            await _repositoryService.DownloadEntityAsync(RepositoryType.Services, RepositoryEntityUid.Parse(uid));
        }

        [HttpPost]
        [Route("/api/v1/repositories/tools/{uid}/download")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task DownloadToolEntity(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            await _repositoryService.DownloadEntityAsync(RepositoryType.Tools, RepositoryEntityUid.Parse(uid));
        }

        [HttpPost]
        [Route("/api/v1/repositories/automations/{uid}/download")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task DownloadAutomationEntity(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            await _repositoryService.DownloadEntityAsync(RepositoryType.Automations, RepositoryEntityUid.Parse(uid));
        }
    }
}
