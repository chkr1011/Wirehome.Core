namespace Wirehome.Core.Repository
{
    public class PackageRegistryServiceOptions
    {
        public const string Filename = "PackageRegistryServiceConfiguration.json";

        public string RootPath { get; set; }

        public string OfficialRepositoryBaseUri { get; set; } = "https://api.github.com/repos/chkr1011/Wirehome.Packages/contents";
    }
}
