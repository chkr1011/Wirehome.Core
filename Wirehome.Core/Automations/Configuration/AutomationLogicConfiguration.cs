using System.Collections.Generic;
using Wirehome.Core.Packages;

namespace Wirehome.Core.Automations.Configuration
{
    public class AutomationLogicConfiguration
    {
        public PackageUid Uid { get; set; }

        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();
    }
}
