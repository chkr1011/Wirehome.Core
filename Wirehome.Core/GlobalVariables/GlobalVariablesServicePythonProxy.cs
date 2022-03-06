#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using Wirehome.Core.Python;

namespace Wirehome.Core.GlobalVariables;

public sealed class GlobalVariablesServicePythonProxy : IInjectedPythonProxy
{
    readonly GlobalVariablesService _globalVariablesService;

    public GlobalVariablesServicePythonProxy(GlobalVariablesService globalVariablesService)
    {
        _globalVariablesService = globalVariablesService ?? throw new ArgumentNullException(nameof(globalVariablesService));
    }

    public string ModuleName { get; } = "global_variables";

    public void delete(string uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        _globalVariablesService.DeleteValue(uid);
    }

    public bool exists(string uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        return _globalVariablesService.ValueExists(uid);
    }

    public object get(string uid, object default_value = null)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        return _globalVariablesService.GetValue(uid, default_value);
    }

    public void set(string uid, object value)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        _globalVariablesService.SetValue(uid, value);
    }
}