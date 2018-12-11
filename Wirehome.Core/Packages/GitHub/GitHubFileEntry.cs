using Newtonsoft.Json;

namespace Wirehome.Core.Packages.GitHub
{
    public class GitHubFileEntry
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }

        [JsonProperty("download_url")]
        public string DownloadUrl { get; set; }
    }
}
