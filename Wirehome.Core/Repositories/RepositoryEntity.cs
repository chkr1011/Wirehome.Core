namespace Wirehome.Core.Repositories
{
    public class RepositoryEntity
    {
        public RepositoryEntityUid Uid { get; set; }

        public RepositoryEntityMetaData MetaData { get; set; }

        public string Description { get; set; }

        public string ReleaseNotes { get; set; }

        public string Script { get; set; }
    }
}
