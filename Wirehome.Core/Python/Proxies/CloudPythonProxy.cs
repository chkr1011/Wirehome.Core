using System;
using IronPython.Runtime;
using Wirehome.Core.Cloud;

#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace Wirehome.Core.Python.Proxies
{
    public class CloudPythonProxy : IPythonProxy
    {
        private readonly CloudService _cloudService;

        public CloudPythonProxy(CloudService cloudService)
        {
            _cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));
        }

        public string ModuleName { get; } = "cloud";

        public void register_message_handler(string type, Func<PythonDictionary, PythonDictionary> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _cloudService.RegisterMessageHandler(type, p => handler(p));
        }
    }
}
