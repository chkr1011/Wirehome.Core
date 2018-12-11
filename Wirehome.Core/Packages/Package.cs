namespace Wirehome.Core.Packages
{
    public class Package
    {
        public PackageUid Uid { get; set; }

        public PackageMetaData MetaData { get; set; }

        public string Description { get; set; }

        public string ReleaseNotes { get; set; }

        public string Script { get; set; }
    }
}
