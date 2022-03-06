namespace Wirehome.Core.Packages;

public sealed class PackageManagerServiceOptions
{
    public const string Filename = "PackageManagerServiceConfiguration.json";

    public string OfficialRepositoryBaseUri { get; set; } = "https://api.github.com/repos/chkr1011/Wirehome.Packages/contents";

    public string RootPath { get; set; }
}