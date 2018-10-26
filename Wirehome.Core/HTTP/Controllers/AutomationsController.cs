using System;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Automations;
using Wirehome.Core.Automations.Configuration;

namespace Wirehome.Core.HTTP.Controllers
{
    public class AutomationsController : Controller
    {
        private readonly AutomationsRegistryService _automationsRegistryService;

        public AutomationsController(AutomationsRegistryService automationsRegistryService)
        {
            _automationsRegistryService = automationsRegistryService ?? throw new ArgumentNullException(nameof(automationsRegistryService));
        }

        [HttpPost]
        [Route("api/v1/automations/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void Post(string uid, [FromBody] AutomationConfiguration configuration)
        {
            _automationsRegistryService.InitializeAutomation(uid, configuration);
        }

        [HttpPost]
        [Route("api/v1/automations/{uid}/deactivate")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostDeactivate(string uid)
        {
            _automationsRegistryService.DeactivateAutomation(uid);
        }

        [HttpPost]
        [Route("api/v1/automations/{uid}/activate")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostActivate(string uid)
        {
            _automationsRegistryService.ActivateAutomation(uid);
        }
    }
}
