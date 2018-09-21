using System.Collections.Generic;
using Wirehome.Core.Repositories;

namespace Wirehome.Core.ServiceHost.Configuration
{
    public class ServiceConfiguration
    {
        public RepositoryEntityUid Uid { get; set; }

        public bool IsEnabled { get; set; }

        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();
    }
}
