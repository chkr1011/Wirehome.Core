#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using IronPython.Runtime;
using System;
using Wirehome.Core.Python;

namespace Wirehome.Core.App
{
    public class AppServicePythonProxy : IInjectedPythonProxy
    {
        readonly AppService _appService;

        public AppServicePythonProxy(AppService appService)
        {
            _appService = appService ?? throw new ArgumentNullException(nameof(appService));
        }

        public delegate object StatusProvider();

        public string ModuleName { get; } = "app";

        public void register_panel(PythonDictionary panel_definition)
        {
            if (panel_definition is null) throw new ArgumentNullException(nameof(panel_definition));

            _appService.RegisterPanel(new AppPanelDefinition
            {
                Uid = panel_definition.get("uid", null) as string,
                PositionIndex = (int)panel_definition.get("position_index", 0),
                ViewSource = (string)panel_definition.get("view_source", null)
            });
        }

        public bool unregister_panel(string uid)
        {
            return _appService.UnregisterPanel(uid);
        }

        public bool panel_registered(string uid)
        {
            return _appService.PanelRegistered(uid);
        }

        public void register_status_provider(string uid, StatusProvider provider)
        {
            _appService.RegisterStatusProvider(uid, () => provider());
        }

        public void unregister_status_provider(string uid)
        {
            _appService.UnregisterStatusProvider(uid);
        }
    }
}