using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Wirehome.Core.Repository.GitHub
{
    // TODO: Do not write to repo directly. Get "Temp" path from StorageService
    // TODO: Or load all files into memory and return the result and the repo service will store all.
    public class GitHubRepositoryEntityDownloader
    {
        private readonly RepositoryServiceOptions _settings;
        private readonly ILogger _logger;

        public GitHubRepositoryEntityDownloader(RepositoryServiceOptions settings, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _logger = loggerFactory.CreateLogger<GitHubRepositoryEntityDownloader>();
        }

        public async Task DownloadAsync(RepositoryEntityUid uid, string targetPath)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
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

                var uri = $"{_settings.OfficialRepositoryBaseUri}/{uid.Id}/{uid.Version}";
                _logger.Log(LogLevel.Information, $"Downloading file list from '{uri}'.");

                var fileListContent = await httpClient.GetStringAsync(uri).ConfigureAwait(false);
                var fileList = JsonConvert.DeserializeObject<List<GitHubFileEntry>>(fileListContent);
                
                foreach (var fileEntry in fileList)
                {
                    uri = fileEntry.DownloadUrl;
                    _logger.Log(LogLevel.Information, $"Downloading file '{uri}' (Size = {fileEntry.Size} bytes).");

                    var fileContent = await httpClient.GetByteArrayAsync(uri).ConfigureAwait(false);
                    var filename = Path.Combine(tempPath, fileEntry.Name);

                    File.WriteAllBytes(filename, fileContent);
                }

                if (Directory.Exists(targetPath))
                {
                    Directory.Delete(targetPath, true);
                }
                
                Directory.Move(tempPath, targetPath);
            }
        }
    }
}
