using Wirehome.Core.Repositories;

namespace Wirehome.Core.Components.Adapters
{
    public class RepositoryEntity
    {
        public RepositoryEntityUid Id { get; set; }

        public string Caption { get; set; }

        public string Description { get; set; }

        public string Author { get; set; }

        public string ReleaseNotes { get; set; }

        public string Script { get; set; }
    }
}
