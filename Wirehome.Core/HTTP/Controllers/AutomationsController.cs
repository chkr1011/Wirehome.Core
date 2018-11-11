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
        private readonly AutomationRegistryService _automationRegistryService;

        public AutomationsController(AutomationRegistryService automationRegistryService)
        {
            _automationRegistryService = automationRegistryService ?? throw new ArgumentNullException(nameof(automationRegistryService));
        }

        [HttpGet]
        [Route("api/v1/automations/uids")]
        [ApiExplorerSettings(GroupName = "v1")]
        public List<string> GetAutomationUids()
        {
            return _automationRegistryService.GetAutomationUids();
        }

        [HttpGet]
        [Route("api/v1/automations")]
        [ApiExplorerSettings(GroupName = "v1")]
        public List<AutomationInstance> GetAutomations()
        {
            return _automationRegistryService.GetAutomations();
        }

        [HttpGet]
        [Route("/api/v1/automations/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public AutomationInstance GetAutomation(string uid)
        {
            try
            {
                return _automationRegistryService.GetAutomation(uid);
            }
            catch (AutomationNotFoundException)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }
        }

        [HttpDelete]
        [Route("/api/v1/automations/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteAutomation(string uid)
        {
            _automationRegistryService.DeleteAutomation(uid);
        }

        [HttpGet]
        [Route("api/v1/automations/{uid}/configuration")]
        [ApiExplorerSettings(GroupName = "v1")]
        public AutomationConfiguration GetConfiguration(string uid)
        {
            try
            {
                return _automationRegistryService.ReadAutomationConfiguration(uid);
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
            _automationRegistryService.WriteAutomationConfiguration(uid, configuration);
        }

        [HttpGet]
        [Route("/api/v1/automations/{automationUid}/settings/{settingUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object GetSetting(string automationUid, string settingUid)
        {
            return _automationRegistryService.GetAutomationSetting(automationUid, settingUid);
        }

        [HttpPost]
        [Route("/api/v1/automations/{automationUid}/settings/{settingUid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostSettingValue(string automationUid, string settingUid, [FromBody] object value)
        {
            _automationRegistryService.SetAutomationSetting(automationUid, settingUid, value);
        }

        [HttpPost]
        [Route("api/v1/automations/{uid}/initialize")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostInitialize(string uid)
        {
            _automationRegistryService.TryInitializeAutomation(uid);
        }

        [HttpPost]
        [Route("api/v1/automations/{uid}/activate")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostActivate(string uid)
        {
            _automationRegistryService.ActivateAutomation(uid);
        }

        [HttpPost]
        [Route("api/v1/automations/{uid}/deactivate")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostDeactivate(string uid)
        {
            _automationRegistryService.DeactivateAutomation(uid);
        }

        [HttpGet]
        [Route("api/v1/automations/{uid}/status")]
        [ApiExplorerSettings(GroupName = "v1")]
        public WirehomeDictionary GetStatus(string uid)
        {
            return _automationRegistryService.GetAutomation(uid).GetStatus();
        }
    }
}
