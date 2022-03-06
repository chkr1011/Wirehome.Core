using Newtonsoft.Json;

namespace Wirehome.Core.Packages.GitHub;

public sealed class GitHubItem
{
    [JsonProperty("download_url")]
    public string DownloadUrl { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("size")]
    public int Size { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }
}