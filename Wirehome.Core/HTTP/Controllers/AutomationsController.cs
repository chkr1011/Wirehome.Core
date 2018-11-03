using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Automations;
using Wirehome.Core.Automations.Configuration;
using Wirehome.Core.Automations.Exceptions;
using Wirehome.Core.Model;

namespace Wirehome.Core.HTTP.Controllers
{
    public class AutomationsController : Controller
    {
        private readonly AutomationRegistryService _automationsRegistryService;

        public AutomationsController(AutomationRegistryService automationsRegistryService)
        {
            _automationsRegistryService = automationsRegistryService ?? throw new ArgumentNullException(nameof(automationsRegistryService));
        }

        [HttpGet]
        [Route("api/v1/automations")]
        [ApiExplorerSettings(GroupName = "v1")]
        public List<Automation> GetAutomations()
        {
            return _automationsRegistryService.GetAutomations();
        }

        [HttpGet]
        [Route("/api/v1/automations/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public Automation GetAutomation(string uid)
        {
            try
            {
                return _automationsRegistryService.GetAutomation(uid);
            }
            catch (AutomationNotFoundException)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }
        }

        [HttpGet]
        [Route("api/v1/automations/{uid}/configuration")]
        [ApiExplorerSettings(GroupName = "v1")]
        public AutomationConfiguration GetConfiguration(string uid)
        {
            try
            {
                return _automationsRegistryService.ReadAutomationConfiguration(uid);
            }
            catch (AutomationNotFoundException)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }
        }

        [HttpPost]
        [Route("api/v1/automations/{uid}/configuration")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostConfiguration(string uid, [FromBody] AutomationConfiguration configuration)
        {
            _automationsRegistryService.WriteAutomationConfiguration(uid, configuration);
        }

        [HttpGet]
        [Route("/api/v1/automations/{automationUid}/settings/{settingUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object GetSetting(string automationUid, string settingUid)
        {
            return _automationsRegistryService.GetAutomationSetting(automationUid, settingUid);
        }

        [HttpPost]
        [Route("/api/v1/automations/{automationUid}/settings/{settingUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostSettingValue(string automationUid, string settingUid, [FromBody] object value)
        {
            _automationsRegistryService.SetAutomationSetting(automationUid, settingUid, value);
        }

        [HttpPost]
        [Route("api/v1/automations/{uid}/activate")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostActivate(string uid)
        {
            _automationsRegistryService.ActivateAutomation(uid);
        }

        [HttpPost]
        [Route("api/v1/automations/{uid}/deactivate")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostDeactivate(string uid)
        {
            _automationsRegistryService.DeactivateAutomation(uid);
        }

        [HttpGet]
        [Route("api/v1/automations/{uid}/status")]
        [ApiExplorerSettings(GroupName = "v1")]
        public WirehomeDictionary GetStatus(string uid)
        {
            return _automationsRegistryService.GetAutomation(uid).GetStatus();
        }
    }
}
