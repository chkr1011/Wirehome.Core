#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using System.Collections.Generic;
using System.Diagnostics;
using IronPython.Runtime;
using Microsoft.Scripting.Utils;

namespace Wirehome.Core.Python.Proxies;

public class DebuggerPythonProxy : IPythonProxy
{
    readonly List<object> _trace = new();
    bool _isEnabled;

    public string ModuleName { get; } = "debugger";

    public void clear_trace()
    {
        lock (_trace)
        {
            _trace.Clear();
        }
    }

    public void disable()
    {
        _isEnabled = false;

        clear_trace();
    }

    public void enable()
    {
        _isEnabled = true;
    }

    public PythonList get_trace()
    {
        lock (_trace)
        {
            var trace = new PythonList();
            trace.AddRange(_trace);
            return trace;
        }
    }

    public string get_trace_string()
    {
        lock (_trace)
        {
            return string.Join(Environment.NewLine, _trace);
        }
    }

    public void halt()
    {
        if (!_isEnabled)
        {
            return;
        }

        if (Debugger.IsAttached)
        {
            Debugger.Break();
        }
    }

    public void trace(object value)
    {
        if (!_isEnabled)
        {
            return;
        }

        lock (_trace)
        {
            _trace.Add(value);
        }
    }
}