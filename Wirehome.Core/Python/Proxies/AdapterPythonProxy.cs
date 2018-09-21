#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using Wirehome.Core.Model;

namespace Wirehome.Core.Python.Proxies
{
    public class LogicPythonProxy : IPythonProxy
    {
        private readonly Func<WirehomeDictionary, WirehomeDictionary> _callback;

        public LogicPythonProxy(Func<WirehomeDictionary, WirehomeDictionary> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public string ModuleName { get; } = "adapter";

        public WirehomeDictionary publish_adapter_message(WirehomeDictionary properties)
        {
            return _callback.Invoke(properties);
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles