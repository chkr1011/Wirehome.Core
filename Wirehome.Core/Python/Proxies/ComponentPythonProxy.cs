#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using Wirehome.Core.Components;

namespace Wirehome.Core.Python.Proxies
{
    public class ComponentPythonProxy : IPythonProxy
    {
        private readonly ComponentRegistryService _componentRegistryService;
        private readonly string _componentUid;

        public ComponentPythonProxy(string componentUid, ComponentRegistryService componentRegistryService)
        {
            _componentUid = componentUid ?? throw new ArgumentNullException(nameof(componentUid));
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
        }

        public string ModuleName => "component";

        public string get_uid()
        {
            return _componentUid;
        }

        // TODO: Delete!
        public object get_property(string statusUid)
        {
            return _componentRegistryService.GetComponentStatus(_componentUid, statusUid);
        }

        // TODO: Delete!
        public void set_property(string statusUid, object value)
        {
            _componentRegistryService.SetComponentStatus(_componentUid, statusUid, value);
        }

        public object get_status(string statusUid)
        {
            return _componentRegistryService.GetComponentStatus(_componentUid, statusUid);
        }

        public void set_status(string statusUid, object value)
        {
            _componentRegistryService.SetComponentStatus(_componentUid, statusUid, value);
        }

        public object get_setting(string settingUid)
        {
            return _componentRegistryService.GetComponentSetting(_componentUid, settingUid);
        }

        public void set_setting(string settingUid, object value)
        {
            _componentRegistryService.SetComponentSetting(_componentUid, settingUid, value);
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles