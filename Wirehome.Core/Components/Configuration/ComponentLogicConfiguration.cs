using System.Collections.Generic;
using Wirehome.Core.Packages;

namespace Wirehome.Core.Components.Configuration
{
    public sealed class ComponentLogicConfiguration
    {
        public PackageUid Uid { get; set; }

        public Dictionary<string, object> Variables { get; set; } = new();

        public ComponentAdapterConfiguration Adapter { get; set; }
    }
}
