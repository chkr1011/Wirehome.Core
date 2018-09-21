using Wirehome.Core.Repositories;

namespace Wirehome.Core.Components.Configuration
{
    public class ComponentLogicConfiguration
    {
        public RepositoryEntityUid Uid { get; set; }

        public ComponentAdapterConfiguration Adapter { get; set; }
    }
}
