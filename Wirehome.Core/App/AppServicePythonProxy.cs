#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using IronPython.Runtime;
using System;
using Wirehome.Core.Python;
using Wirehome.Core.Python.SDK;

namespace Wirehome.Core.App
{
    public class AppServicePythonProxy : IInjectedPythonProxy
    {
        private readonly AppService _appService;

        public AppServicePythonProxy(AppService appService)
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

        public void register_status_provider(string uid, Func<object> provider)
        {
            _appService.RegisterStatusProvider(uid, provider);
        }

        public void unregister_status_provider(string uid)
        {
            _appService.UnregisterStatusProvider(uid);
        }
    }
}