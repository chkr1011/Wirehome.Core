using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wirehome.Core.Contracts;
using Wirehome.Core.Packages.Exceptions;
using Wirehome.Core.Packages.GitHub;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Packages
{
    public class PackageManagerService : IService
    {
        private const string PackagesDirectory = "Packages";

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

        public bool PackageExists(PackageUid uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var path = GetPackageRootPath(uid);
            return Directory.Exists(path);
        }

        public Task DownloadPackageAsync(PackageUid uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _storageService.TryReadOrCreate(out PackageManagerServiceOptions options, PackageManagerServiceOptions.Filename);

            var downloader = new GitHubRepositoryPackageDownloader(options, _logger);
            return downloader.DownloadAsync(uid, GetPackageRootPath(uid));
        }

        public Task ForkPackageAsync(PackageUid packageUid, PackageUid packageForkUid)
        {
            if (packageUid == null) throw new ArgumentNullException(nameof(packageUid));
            if (packageForkUid == null) throw new ArgumentNullException(nameof(packageForkUid));

            if (!PackageExists(packageUid))
            {
                throw new WirehomePackageNotFoundException(packageUid);
            }

            if (PackageExists(packageForkUid))
            {
                throw new InvalidOperationException($"Package '{packageForkUid}' already exists.");
            }

            var sourcePath = GetPackageRootPath(packageUid);
            var destinationPath = GetPackageRootPath(packageForkUid);

            Directory.CreateDirectory(destinationPath);

            foreach (var directory in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(directory.Replace(sourcePath, destinationPath));
            }

            foreach (var file in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(file, file.Replace(sourcePath, destinationPath), true);
            }

            return Task.CompletedTask;
        }

        public async Task<List<PackageUid>> FetchRemotePackageUidsAsync()
        {
            await Task.CompletedTask;
            // TODO: Implement.
            return new List<PackageUid>();
        }

        public List<PackageUid> GetPackageUids()
        {
            var packageUids = new List<PackageUid>();

            var packagesRootPath = GetPackagesRootPath();
            foreach (var packageId in _storageService.EnumerateDirectories("*", packagesRootPath))
            {
                if (packageId.StartsWith("."))
                {
                    continue;
                }

                foreach (var packageVersion in _storageService.EnumerateDirectories("*", packagesRootPath, packageId))
                {
                    packageUids.Add(new PackageUid(packageId, packageVersion));
                }
            }

            return packageUids;
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

        public PackageMetaData GetPackageMetaData(PackageUid uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            return LoadPackage(uid).MetaData;
        }

        public string GetPackageDescription(PackageUid uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            return LoadPackage(uid).Description;
        }

        public string GetPackageReleaseNotes(PackageUid uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            return LoadPackage(uid).ReleaseNotes;
        }

        public string GetPackageRootPath(PackageUid uid)
        {
            var path = GetPackagesRootPath();

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

        private string GetPackagesRootPath()
        {
            _storageService.TryReadOrCreate(out PackageManagerServiceOptions options, PackageManagerServiceOptions.Filename);

            var rootPath = options.RootPath;
            if (string.IsNullOrEmpty(rootPath))
            {
                rootPath = Path.Combine(_storageService.DataPath, PackagesDirectory);
            }

            return rootPath;
        }

        private static string GetLatestVersionPath(string rootPath, string id)
        {
            rootPath = Path.Combine(rootPath, id);

            if (!Directory.Exists(rootPath))
            {
                throw new WirehomePackageNotFoundException(PackageUid.Parse(id));
            }

            var versions = Directory.GetDirectories(rootPath)
                .OrderByDescending(d => d.ToLowerInvariant());

            return versions.First();
        }

        private static Package LoadPackage(PackageUid uid, string path)
        {
            if (!Directory.Exists(path))
            {
                throw new WirehomePackageNotFoundException(uid);
            }

            var source = new Package();

            var metaFile = Path.Combine(path, "meta.json");
            if (!File.Exists(metaFile))
            {
                throw new WirehomePackageException($"Package directory '{path}' contains no 'meta.json'.");
            }

            try
            {
                var metaData = File.ReadAllText(metaFile, Encoding.UTF8);
                source.MetaData = JsonConvert.DeserializeObject<PackageMetaData>(metaData);
            }
            catch (Exception exception)
            {
                throw new WirehomePackageException("Unable to parse 'meta.json'.", exception);
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
