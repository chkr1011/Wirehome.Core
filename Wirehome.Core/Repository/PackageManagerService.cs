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
    public class PackageManagerService : IService
    {
        private readonly StorageService _storageService;
        private readonly ILogger _logger;

        public PackageManagerService(StorageService storageService, ILogger<PackageManagerService> logger)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Start()
        {
            
        }

        public Package LoadPackage(PackageUid uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            if (string.IsNullOrEmpty(uid.Id))
            {
                throw new ArgumentException("The ID of the package UID is not set.");
            }

            var path = GetPackageRootPath(uid);
            var source = LoadPackage(uid, path);

            return source;
        }

        public Task DownloadPackageAsync(PackageUid uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _storageService.TryReadOrCreate(out PackageManagerServiceOptions options, PackageManagerServiceOptions.Filename);

            var downloader = new GitHubRepositoryPackageDownloader(options, _logger);
            return downloader.DownloadAsync(uid, GetPackageRootPath(uid));
        }

        public async Task ForkPackageAsync(PackageUid packageUid, PackageUid packageForkUid)
        {
            
        }

        public void DeletePackage(PackageUid uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var rootPath = GetPackageRootPath(uid);
            if (!Directory.Exists(rootPath))
            {
                return;
            }

            Directory.Delete(rootPath, true);
            _logger.Log(LogLevel.Information, $"Deleted package '{uid}'.");
        }

        public PackageMetaData GetMetaData(PackageUid uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            return LoadPackage(uid).MetaData;
        }

        public string GetDescription(PackageUid uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            return LoadPackage(uid).Description;
        }

        public string GetReleaseNotes(PackageUid uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            return LoadPackage(uid).ReleaseNotes;
        }

        private string GetPackageRootPath(PackageUid uid)
        {
            _storageService.TryRead(out PackageManagerServiceOptions options, PackageManagerServiceOptions.Filename);
            
            var rootPath = options.RootPath;
            if (string.IsNullOrEmpty(rootPath))
            {
                rootPath = Path.Combine(_storageService.DataPath, "Packages");
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

        private static Package LoadPackage(PackageUid uid, string path)
        {
            if (!Directory.Exists(path))
            {
                throw new WirehomeRepositoryPackageNotFoundException(uid);
            }

            var source = new Package();

            var metaFile = Path.Combine(path, "meta.json");
            if (!File.Exists(metaFile))
            {
                throw new WirehomeException($"Package directory '{path}' contains no 'meta.json'.");
            }

            try
            {
                var metaData = File.ReadAllText(metaFile, Encoding.UTF8);
                source.MetaData = JsonConvert.DeserializeObject<PackageMetaData>(metaData);
            }
            catch (Exception exception)
            {
                throw new WirehomeRepositoryException("Unable to parse 'meta.json'.", exception);
            }

            source.Uid = new PackageUid
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
