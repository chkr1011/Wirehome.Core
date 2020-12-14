using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Wirehome.Core.Packages.GitHub
{
    public class GitHubRepositoryPackageDownloader
    {
        readonly PackageManagerServiceOptions _options;
        readonly ILogger _logger;

        public GitHubRepositoryPackageDownloader(PackageManagerServiceOptions options, ILogger logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task DownloadAsync(PackageUid packageUid, string targetPath)
        {
            if (packageUid == null) throw new ArgumentNullException(nameof(packageUid));
            if (targetPath == null) throw new ArgumentNullException(nameof(targetPath));

            var tempPath = targetPath + "_downloading";
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }

            Directory.CreateDirectory(tempPath);

            using (var httpClient = new HttpClient())
            {
                // The User-Agent is mandatory for using the GitHub API.
                // https://developer.github.com/v3/?#user-agent-required
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Wirehome.Core");

                var rootItem = new GitHubItem
                {
                    Type = "dir",
                    DownloadUrl = $"{_options.OfficialRepositoryBaseUri}/{packageUid.Id}/{packageUid.Version}"
                };

                await DownloadDirectoryAsync(httpClient, rootItem, tempPath).ConfigureAwait(false);
            }

            if (Directory.Exists(targetPath))
            {
                Directory.Delete(targetPath, true);
            }

            Directory.Move(tempPath, targetPath);
        }

        async Task DownloadFileAsync(HttpClient httpClient, GitHubItem item, string localDirectory)
        {
            var uri = item.DownloadUrl;

            _logger.LogTrace($"Downloading file '{uri}' (Size = {item.Size} bytes).");

            var fileContent = await httpClient.GetByteArrayAsync(uri).ConfigureAwait(false);
            var filename = Path.Combine(localDirectory, item.Name);

            await File.WriteAllBytesAsync(filename, fileContent).ConfigureAwait(false);
        }

        async Task DownloadDirectoryAsync(HttpClient httpClient, GitHubItem item, string localDirectory)
        {
            if (!string.IsNullOrEmpty(item.Name))
            {
                localDirectory = Path.Combine(localDirectory, item.Name);
            }

            if (!Directory.Exists(localDirectory))
            {
                Directory.CreateDirectory(localDirectory);
            }

            _logger.LogTrace($"Downloading item list for directory '{localDirectory}'.");

            var subItemsContent = await httpClient.GetStringAsync(item.DownloadUrl).ConfigureAwait(false);
            var subItems = JsonConvert.DeserializeObject<List<GitHubItem>>(subItemsContent);

            foreach (var subItem in subItems)
            {
                if (subItem.Type == "file")
                {
                    await DownloadFileAsync(httpClient, subItem, localDirectory).ConfigureAwait(false);
                }
                else if (subItem.Type == "dir")
                {
                    subItem.DownloadUrl = item.DownloadUrl.TrimEnd('/') + "/" + subItem.Name;

                    await DownloadDirectoryAsync(httpClient, subItem, localDirectory).ConfigureAwait(false);
                }
            }
        }
    }
}