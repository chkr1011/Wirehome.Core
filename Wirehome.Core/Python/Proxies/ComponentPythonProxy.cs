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

        public object get_status(string status_uid, object default_value = null)
        {
            return PythonConvert.ToPython(_componentRegistryService.GetComponentStatus(_componentUid, status_uid, default_value));
        }

        public void set_status(string status_uid, object value)
        {
            _componentRegistryService.SetComponentStatus(_componentUid, status_uid, PythonConvert.FromPython(value));
        }

        public object get_setting(string setting_uid, object default_value = null)
        {
            return PythonConvert.ToPython(_componentRegistryService.GetComponentSetting(_componentUid, setting_uid, default_value));
        }

        public void register_setting(string settingUid, object value)
        {
            _componentRegistryService.RegisterComponentSetting(_componentUid, settingUid, PythonConvert.FromPython(value));
        }

        public void set_setting(string settingUid, object value)
        {
            _componentRegistryService.SetComponentSetting(_componentUid, settingUid, PythonConvert.FromPython(value));
        }

        public object get_configuration(string configuration_uid, object default_value = null)
        {
            return PythonConvert.ToPython(_componentRegistryService.GetComponentConfiguration(_componentUid, configuration_uid, default_value));
        }

        public void set_configuration(string configuration_uid, object value)
        {
            _componentRegistryService.SetComponentConfiguration(_componentUid, configuration_uid, PythonConvert.FromPython(value));
        }
    }
}