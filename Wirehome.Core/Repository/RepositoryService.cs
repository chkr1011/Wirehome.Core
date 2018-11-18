using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Wirehome.Core.Contracts;
using Wirehome.Core.Exceptions;
using Wirehome.Core.Repository.Exceptions;
using Wirehome.Core.Repository.GitHub;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Repository
{
    public class RepositoryService : IService
    {
        private readonly StorageService _storageService;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public RepositoryService(StorageService storageService, ILoggerFactory loggerFactory)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

            _logger = _loggerFactory.CreateLogger<RepositoryService>();
        }

        public void Start()
        {
            
        }

        public RepositoryEntity LoadEntity(RepositoryEntityUid uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            if (string.IsNullOrEmpty(uid.Id))
            {
                throw new ArgumentException("The ID of the RepositoryEntityUid is not set.");
            }

            var path = GetEntityRootPath(uid);
            var source = LoadEntity(uid, path);

            return source;
        }

        public Task DownloadEntityAsync(RepositoryEntityUid uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _storageService.TryReadOrCreate(out RepositoryServiceOptions options, "RepositoryServiceConfiguration.json");

            var downloader = new GitHubRepositoryEntityDownloader(options, _loggerFactory);
            return downloader.DownloadAsync(uid, GetEntityRootPath(uid));
        }

        public void DeleteEntity(RepositoryEntityUid uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var rootPath = GetEntityRootPath(uid);
            if (!Directory.Exists(rootPath))
            {
                return;
            }

            Directory.Delete(rootPath, true);
            _logger.Log(LogLevel.Information, $"Deleted entity '{uid}'.");
        }

        public RepositoryEntityMetaData GetMetaData(RepositoryEntityUid uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            return LoadEntity(uid).MetaData;
        }

        public string GetDescription(RepositoryEntityUid uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            return LoadEntity(uid).Description;
        }

        public string GetReleaseNotes(RepositoryEntityUid uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            return LoadEntity(uid).ReleaseNotes;
        }

        private string GetEntityRootPath(RepositoryEntityUid uid)
        {
            _storageService.TryRead(out RepositoryServiceOptions options, RepositoryServiceOptions.Filename);
            
            var rootPath = options.RootPath;
            if (string.IsNullOrEmpty(rootPath))
            {
                rootPath = Path.Combine(_storageService.DataPath, "Repository");
            }

            var path = rootPath;

            if (string.IsNullOrEmpty(uid.Version))
            {
                path = GetLatestVersionPath(path, uid.Id);
            }
            else
            {
                path = Path.Combine(path, uid.Id, uid.Version);
            }

            return path;
        }

        private static string GetLatestVersionPath(string rootPath, string id)
        {
            rootPath = Path.Combine(rootPath, id);

            var versions = Directory.GetDirectories(rootPath)
                .OrderByDescending(d => d.ToLowerInvariant());

            return versions.First();
        }

        private static RepositoryEntity LoadEntity(RepositoryEntityUid uid, string path)
        {
            if (!Directory.Exists(path))
            {
                throw new WirehomeRepositoryEntityNotFoundException(uid);
            }

            var source = new RepositoryEntity();

            var metaFile = Path.Combine(path, "meta.json");
            if (!File.Exists(metaFile))
            {
                throw new WirehomeException($"Repository entity directory '{path}' contains no 'meta.json'.");
            }

            try
            {
                var metaData = File.ReadAllText(metaFile, Encoding.UTF8);
                source.MetaData = JsonConvert.DeserializeObject<RepositoryEntityMetaData>(metaData);
            }
            catch (Exception exception)
            {
                throw new WirehomeRepositoryException("Unable to parse 'meta.json'.", exception);
            }

            source.Uid = new RepositoryEntityUid
            {
                Id = Directory.GetParent(path).Name,
                Version = new DirectoryInfo(path).Name
            };

            source.Description = ReadFileContent(path, "description.md");
            source.ReleaseNotes = ReadFileContent(path, "releaseNotes.md");
            source.Script = ReadFileContent(path, "script.py");

            return source;
        }

        private static string ReadFileContent(string path, string filename)
        {
            var descriptionFile = Path.Combine(path, filename);
            if (!File.Exists(descriptionFile))
            {
                return string.Empty;
            }

            return File.ReadAllText(descriptionFile, Encoding.UTF8);
        }
    }
}
