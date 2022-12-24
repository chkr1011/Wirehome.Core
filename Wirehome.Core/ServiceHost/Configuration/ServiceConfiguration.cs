using System.Collections.Generic;

namespace Wirehome.Core.ServiceHost.Configuration;

public sealed class ServiceConfiguration
{
    public bool DelayedStart { get; set; }

    public bool IsEnabled { get; set; } = true;

    public Dictionary<string, object> Variables { get; set; } = new();

    public string Version { get; set; }
}