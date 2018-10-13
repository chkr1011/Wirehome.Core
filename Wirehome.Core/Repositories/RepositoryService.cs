using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Wirehome.Core.Exceptions;
using Wirehome.Core.Repositories.Exceptions;
using Wirehome.Core.Repositories.GitHub;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Repositories
{
    public class RepositoryService
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

        public RepositoryEntity LoadEntity(RepositoryType type, RepositoryEntityUid uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            if (string.IsNullOrEmpty(uid.Id))
            {
                throw new ArgumentException("The ID of the RepositoryEntityUid is not set.");
            }

            var path = GetEntityRootPath(type, uid);
            var source = LoadEntity(uid, path);
            
            return source;
        }
        
        public async Task DownloadEntityAsync(RepositoryType type, RepositoryEntityUid uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            if (!_storageService.TryRead(out RepositoryServiceSettings settings, "RepositoryService.json"))
            {
                settings = new RepositoryServiceSettings();
            }

            var downloader = new GitHubRepositoryEntityDownloader(settings, _loggerFactory);
            await downloader.DownloadAsync(type, uid, GetEntityRootPath(type, uid)).ConfigureAwait(false);
        }
        
        public void DeleteEntity(RepositoryType type, RepositoryEntityUid uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var rootPath = GetEntityRootPath(type, uid);
            if (!Directory.Exists(rootPath))
            {
                return;
            }

            Directory.Delete(rootPath, true);
            _logger.Log(LogLevel.Information, $"Deleted entity '{uid}' of type '{type}'.");
        }

        public static string GetPathForType(RepositoryType type)
        {
            if (type == RepositoryType.Automations)
            {
                return "automations";
            }

            if (type == RepositoryType.ComponentAdapters)
            {
                return "component_adapters";
            }

            if (type == RepositoryType.ComponentLogics)
            {
                return "component_logics";
            }

            if (type == RepositoryType.Macros)
            {
                return "macros";
            }

            if (type == RepositoryType.Services)
            {
                return "services";
            }

            if (type == RepositoryType.Tools)
            {
                return "tools";
            }

            throw new NotSupportedException();
        }

        public string GetEntityRootPath(RepositoryType type, RepositoryEntityUid uid)
        {
            if (!_storageService.TryRead(out RepositoryServiceSettings settings, "RepositoryService.json"))
            {
                settings = new RepositoryServiceSettings();
            }

            var rootPath = settings.RootPath;
            if (string.IsNullOrEmpty(rootPath))
            {
                rootPath = Path.Combine(_storageService.DataPath, "Repositories");
            }

            var typePath = GetPathForType(type);
            var path = Path.Combine(rootPath, typePath);

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
                throw new WirehomeException($"Repository entity '{uid}' not found.");
            }

            var source = new RepositoryEntity();

            var metaFile = Path.Combine(path, "meta.json");
            if (!File.Exists(metaFile))
            {
                throw new WirehomeException($"Repository entity directory '{path}' contains no 'meta.json'.");
            }

            try
            {
                source.MetaData = JsonConvert.DeserializeObject<RepositoryEntityMetaData>(File.ReadAllText(metaFile, Encoding.UTF8));
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
