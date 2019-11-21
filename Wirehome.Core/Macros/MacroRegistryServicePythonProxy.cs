#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using IronPython.Runtime;
using System;
using Wirehome.Core.Python;

namespace Wirehome.Core.Macros
{
    public class MacroRegistryServicePythonProxy : IInjectedPythonProxy
    {
        private readonly MacroRegistryService _macroRegistryService;

        public MacroRegistryServicePythonProxy(MacroRegistryService macroRegistryService)
        {
            _macroRegistryService = macroRegistryService ?? throw new ArgumentNullException(nameof(macroRegistryService));
        }

        public string ModuleName => "macro_registry";

        public List get_uids()
        {
            var result = new List();
            foreach (var componentUid in _macroRegistryService.GetMacroUids())
            {
                result.Add(componentUid);
            }

            return result;
        }

        public PythonDictionary execute_macro(string uid)
        {
            return _macroRegistryService.ExecuteMacro(uid);
        }
    }
}