#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using Wirehome.Core.Python;

namespace Wirehome.Core.Automations
{
    public class AutomationPythonProxy : IPythonProxy
    {
        private readonly AutomationRegistryService _automationRegistryService;
        private readonly string _automationUid;

        public AutomationPythonProxy(string automationUid, AutomationRegistryService automationRegistryService)
        {
            _automationUid = automationUid ?? throw new ArgumentNullException(nameof(automationUid));
            _automationRegistryService = automationRegistryService ?? throw new ArgumentNullException(nameof(automationRegistryService));
        }

        public string ModuleName => "automation";

        public string get_uid()
        {
            return _automationUid;
        }

        public object get_setting(string setting_uid, object default_value = null)
        {
            return PythonConvert.ToPython(_automationRegistryService.GetAutomationSetting(_automationUid, setting_uid, default_value));
        }
        
        public void set_setting(string settingUid, object value)
        {
            _automationRegistryService.SetAutomationSetting(_automationUid, settingUid, PythonConvert.FromPython(value));
        }     
    }
}