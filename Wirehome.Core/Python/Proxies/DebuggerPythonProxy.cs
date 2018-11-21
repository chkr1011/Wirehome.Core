#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using IronPython.Runtime;
using Microsoft.Scripting.Utils;

namespace Wirehome.Core.Python.Proxies
{
    public class DebuggerPythonProxy : IPythonProxy
    {
        private readonly ConcurrentBag<object> _trace = new ConcurrentBag<object>();
        private bool _isEnabled;

        public string ModuleName { get; } = "debugger";
        
        public void enable()
        {
            _isEnabled = true;
        }

        public void disable()
        {
            _isEnabled = false;
            _trace.Clear();
        }

        public string get_trace_string()
        {
            return string.Join(Environment.NewLine, _trace);
        }

        public List get_trace()
        {
            var trace = new List();
            trace.AddRange(_trace);

            return trace;
        }

        public void trace(object @object)
        {
            if (!_isEnabled)
            {
                return;
            }

            _trace.Add(@object);
        }

        public void clear_trace()
        {
            _trace.Clear();
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
