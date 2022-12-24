using System.Collections.Generic;
using Wirehome.Core.Packages;

namespace Wirehome.Core.Components.Configuration;

public sealed class ComponentLogicConfiguration
{
    public ComponentAdapterConfiguration Adapter { get; set; }
    public PackageUid Uid { get; set; }

    public Dictionary<string, object> Variables { get; set; } = new();
}