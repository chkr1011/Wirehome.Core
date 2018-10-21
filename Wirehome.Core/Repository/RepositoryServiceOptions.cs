namespace Wirehome.Core.Repository
{
    public class RepositoryServiceOptions
    {
        public const string Filename = "RepositoryServiceConfiguration.json";

        public string RootPath { get; set; }

        public string OfficialRepositoryBaseUri { get; set; } = "https://api.github.com/repos/chkr1011/Wirehome.Repositories/contents";
    }
}
