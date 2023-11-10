#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

using System;
using IronPython.Runtime;
using Wirehome.Core.Python;

namespace Wirehome.Core.Macros;

public sealed class MacroRegistryServicePythonProxy : IInjectedPythonProxy
{
    readonly MacroRegistryService _macroRegistryService;

    public MacroRegistryServicePythonProxy(MacroRegistryService macroRegistryService)
    {
        _macroRegistryService = macroRegistryService ?? throw new ArgumentNullException(nameof(macroRegistryService));
    }

    public string ModuleName => "macro_registry";

    public PythonDictionary execute_macro(string uid)
    {
        return PythonConvert.ToPythonDictionary(_macroRegistryService.ExecuteMacro(uid));
    }

    public PythonList get_uids()
    {
        var result = new PythonList();
        foreach (var componentUid in _macroRegistryService.GetMacroUids())
        {
            result.Add(componentUid);
        }

        return result;
    }
}