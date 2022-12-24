#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using IronPython.Runtime;
using Wirehome.Core.Python;

namespace Wirehome.Core.Cloud;

public class CloudServicePythonProxy : IInjectedPythonProxy
{
    public delegate PythonDictionary RawMessageHandler(PythonDictionary message);

    readonly CloudService _cloudService;

    public CloudServicePythonProxy(CloudService cloudService)
    {
        _cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));
    }

    public string ModuleName { get; } = "cloud";

    public void register_raw_message_handler(string type, RawMessageHandler handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        _cloudService.RegisterMessageHandler(type, p => handler(PythonConvert.ToPythonDictionary(p)));
    }
}