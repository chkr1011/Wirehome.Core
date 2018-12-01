#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using IronPython.Runtime;
using Wirehome.Core.Components.Exceptions;
using Wirehome.Core.Model;
using Wirehome.Core.Python;

namespace Wirehome.Core.Components
{
    public class MacroRegistryServicePythonProxy : IInjectedPythonProxy
    {
        private readonly ComponentRegistryService _componentRegistryService;

        public MacroRegistryServicePythonProxy(ComponentRegistryService componentRegistryService)
        {
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
        }

        public string ModuleName => "component_registry";

        public List get_uids()
        {
            var result = new List();
            foreach (var componentUid in _componentRegistryService.GetComponentUids())
            {
                result.Add(componentUid);
            }

            return result;
        }

        public bool has_status(string component_uid, string status_uid)
        {
            return _componentRegistryService.ComponentHasStatus(component_uid, status_uid);
        }

        public object get_status(string component_uid, string status_uid, object default_value = null)
        {
            return PythonConvert.ToPython(_componentRegistryService.GetComponentStatus(component_uid, status_uid, default_value));
        }

        public void set_status(string component_uid, string status_uid, object value)
        {
            _componentRegistryService.SetComponentStatus(component_uid, status_uid, PythonConvert.FromPython(value));
        }

        public bool has_setting(string component_uid, string setting_uid)
        {
            return _componentRegistryService.ComponentHasSetting(component_uid, setting_uid);
        }

        public object get_setting(string component_uid, string setting_uid, object default_value = null)
        {
            return PythonConvert.ToPython(_componentRegistryService.GetComponentSetting(component_uid, setting_uid, default_value));
        }

        public void register_setting(string component_uid, string setting_uid, object value)
        {
            _componentRegistryService.RegisterComponentSetting(component_uid, setting_uid, PythonConvert.FromPython(value));
        }

        public void set_setting(string component_uid, string setting_uid, object value)
        {
            _componentRegistryService.SetComponentSetting(component_uid, setting_uid, PythonConvert.FromPython(value));
        }

        [Obsolete]
        public PythonDictionary execute_command(string component_uid, PythonDictionary message)
        {
            return process_message(component_uid, message);
        }

        public PythonDictionary process_message(string component_uid, PythonDictionary message)
        {
            try
            {
                return _componentRegistryService.ProcessComponentMessage(component_uid, message);
            }
            catch (ComponentNotFoundException)
            {
                return new WirehomeDictionary()
                    .WithValue("type", "exception.component_not_found")
                    .WithValue("component_uid", component_uid);
            }
        }
    }
}