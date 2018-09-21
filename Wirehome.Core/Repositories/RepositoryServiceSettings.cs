namespace Wirehome.Core.Repositories
{
    public class RepositoryServiceSettings
    {
        public string RootPath { get; set; }

        public string OfficialRepositoriesBaseUri { get; set; } = "https://api.github.com/repos/chkr1011/Wirehome.Repositories/contents";
    }
}
