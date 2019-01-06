using System;
using IronPython.Runtime;
using Wirehome.Core.Python;
using Wirehome.Core.Python.SDK;

namespace Wirehome.Core.App
{
    public class AppPythonProxy : IInjectedPythonProxy
    {
        private readonly AppService _appService;

        public AppPythonProxy(AppService appService)
        {
            _appService = appService ?? throw new ArgumentNullException(nameof(appService));
        }

        public string ModuleName { get; } = "app";
        
        public void register_panel([PythonDictionaryDefinition(typeof(AppPanelDefinition))] PythonDictionary panel_definition)
        {
            var definition = PythonConvert.CreateModel<AppPanelDefinition>(panel_definition);
            _appService.RegisterPanel(definition);
        }

        public bool unregister_panel(string uid)
        {
            return _appService.UnregisterPanel(uid);
        }

        public bool panel_registered(string uid)
        {
            return _appService.PanelRegistered(uid);
        }
    }
}