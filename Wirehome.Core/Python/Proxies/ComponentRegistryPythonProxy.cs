#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using Wirehome.Core.Components;
using Wirehome.Core.Model;

namespace Wirehome.Core.Python.Proxies
{
    public class ComponentRegistryPythonProxy : IPythonProxy
    {
        private readonly ComponentRegistryService _componentRegistryService;

        public ComponentRegistryPythonProxy(ComponentRegistryService componentRegistryService)
        {
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
        }

        public string ModuleName => "component_registry";

        public object get_status(string componentUid, string statusUid)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (statusUid == null) throw new ArgumentNullException(nameof(statusUid));

            return _componentRegistryService.GetComponentStatus(componentUid, statusUid);
        }

        public void set_status(string componentUid, string statusUid, object value)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (statusUid == null) throw new ArgumentNullException(nameof(statusUid));

            _componentRegistryService.SetComponentStatus(componentUid, statusUid, value);
        }

        public object get_setting(string componentUid, string settingUid)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            return _componentRegistryService.GetComponentSetting(componentUid, settingUid);
        }

        public void set_setting(string componentUid, string settingUid, object value)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (settingUid == null) throw new ArgumentNullException(nameof(settingUid));

            _componentRegistryService.SetComponentSetting(componentUid, settingUid, value);
        }

        public WirehomeDictionary execute_command(string componentUid, WirehomeDictionary message)
        {
            return process_message(componentUid, message);
        }

        public WirehomeDictionary process_message(string componentUid, WirehomeDictionary message)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (message == null) throw new ArgumentNullException(nameof(message));

            return _componentRegistryService.ProcessComponentMessage(componentUid, message);
        }
    }
}