using System.Collections.Generic;
using Wirehome.Core.Repositories;

namespace Wirehome.Core.Components.Configuration
{
    public class ComponentLogicConfiguration
    {
        public RepositoryEntityUid Uid { get; set; }

        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();

        public ComponentAdapterConfiguration Adapter { get; set; }        
    }
}
