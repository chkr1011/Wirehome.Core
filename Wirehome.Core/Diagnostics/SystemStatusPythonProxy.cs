#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using Wirehome.Core.Python;

namespace Wirehome.Core.Diagnostics;

public class SystemStatusPythonProxy : IInjectedPythonProxy
{
    public delegate object ValueProvider();

    readonly SystemStatusService _systemStatusService;

    public SystemStatusPythonProxy(SystemStatusService systemInformationService)
    {
        _systemStatusService = systemInformationService ?? throw new ArgumentNullException(nameof(systemInformationService));
    }

    public string ModuleName { get; } = "system_status";

    public void delete(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        _systemStatusService.Delete(key);
    }

    public object get(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return _systemStatusService.Get(key);
    }

    public void set(string key, object value)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        _systemStatusService.Set(key, value);
    }

    public void set(string key, ValueProvider valueProvider)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        _systemStatusService.Set(key, valueProvider);
    }
}