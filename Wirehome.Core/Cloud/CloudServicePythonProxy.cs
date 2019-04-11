#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using IronPython.Runtime;
using Wirehome.Core.Python;

namespace Wirehome.Core.Cloud
{
    public class CloudServicePythonProxy : IInjectedPythonProxy
    {
        private readonly CloudService _cloudService;

        public CloudServicePythonProxy(CloudService cloudService)
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
