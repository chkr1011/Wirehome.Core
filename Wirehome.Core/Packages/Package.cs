namespace Wirehome.Core.Packages;

public sealed class Package
{
    public string Description { get; set; }

    public PackageMetaData MetaData { get; set; }

    public string ReleaseNotes { get; set; }

    public string Script { get; set; }
    public PackageUid Uid { get; set; }
}