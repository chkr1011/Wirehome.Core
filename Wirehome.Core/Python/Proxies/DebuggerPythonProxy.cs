#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using System.Collections.Generic;
using System.Diagnostics;
using IronPython.Runtime;
using Microsoft.Scripting.Utils;

namespace Wirehome.Core.Python.Proxies
{
    public class DebuggerPythonProxy : IPythonProxy
    {
        private readonly List<object> _trace = new List<object>();
        private bool _isEnabled;

        public string ModuleName { get; } = "debugger";

        public void enable()
        {
            _isEnabled = true;
        }

        public void disable()
        {
            _isEnabled = false;

            clear_trace();
        }

        public string get_trace_string()
        {
            lock (_trace)
            {
                return string.Join(Environment.NewLine, _trace);
            }
        }

        public List get_trace()
        {
            lock (_trace)
            {
                var trace = new List();
                trace.AddRange(_trace);
                return trace;
            }
        }

        public void trace(object @object)
        {
            if (!_isEnabled)
            {
                return;
            }

            lock (_trace)
            {
                _trace.Add(@object);
            }
        }

        public void clear_trace()
        {
            lock (_trace)
            {
                _trace.Clear();
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
    }
}
