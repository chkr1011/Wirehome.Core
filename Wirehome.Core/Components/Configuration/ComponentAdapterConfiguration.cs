using System.Collections.Generic;
using Wirehome.Core.Packages;

namespace Wirehome.Core.Components.Configuration
{
    public class ComponentAdapterConfiguration
    {
        public PackageUid Uid { get; set; }

        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();
    }
}
