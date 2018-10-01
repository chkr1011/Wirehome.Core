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
        public void PostArea(string uid, [FromBody] AutomationConfiguration configuration)
        {
            _automationsRegistryService.InitializeAutomation(uid, configuration);
        }
    }
}
