using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Net.Mime;
using System.Threading.Tasks;
using Wirehome.Core.Contracts;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Backup
{
    public class BackupService : IService
    {
        const string BackupsDirectory = "Backups";
        const string NoBackupIndicatorFile = "no_backup";

        readonly StorageService _storageService;

        public BackupService(StorageService storageService)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        }

        public void Start()
        {
            var backupPath = Path.Combine(_storageService.DataPath, BackupsDirectory);

            ExcludePathFromBackup(backupPath);
        }

        public void ExcludePathFromBackup(string path)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = Path.Combine(path, NoBackupIndicatorFile);
            using (File.Create(path))
            {
            }
        }

        public async Task CreateBackupAsync()
        {
            var now = DateTime.Now;

            var tempFilename = Path.GetTempFileName();

            using (var package = Package.Open(tempFilename, FileMode.OpenOrCreate))
            {
                package.PackageProperties.Title = "Wirehome.Backup";
                package.PackageProperties.Creator = "Wirehome.Core";
                package.PackageProperties.Description = "Backup package for Wirehome.Core data.";
                package.PackageProperties.Created = now;

                foreach (var directory in Directory.GetDirectories(_storageService.DataPath, "*", SearchOption.TopDirectoryOnly))
                {
                    await AddDirectoryToPackage(directory, package).ConfigureAwait(false);
                }

                package.Flush();

                //package.DeletePart(new Uri("/[Content_Types].xml", UriKind.Relative));
            }

            var backupFilename = $"{now.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}{now.ToString("HHmmss", CultureInfo.InvariantCulture)}.zip";
            var backupPath = Path.Combine(_storageService.DataPath, BackupsDirectory);

            if (!Directory.Exists(backupPath))
            {
                Directory.CreateDirectory(backupPath);
            }

            backupPath = Path.Combine(backupPath, backupFilename);

            File.Move(tempFilename, backupPath);
        }

        public List<string> GetBackupUids()
        {
            var uids = new List<string>();

            var backupsPath = Path.Combine(_storageService.DataPath, BackupsDirectory);

            foreach (var file in Directory.GetFiles(backupsPath, "*.zip", SearchOption.TopDirectoryOnly))
            {
                uids.Add(Path.GetFileName(file.Replace(".zip", string.Empty, StringComparison.Ordinal)));
            }

            return uids;
        }

        public string GetBackupFilename(string uid)
        {
            return Path.Combine(_storageService.DataPath, BackupsDirectory, uid + ".zip");
        }

        public void DeleteBackup(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var path = Path.Combine(_storageService.DataPath, BackupsDirectory, uid + ".zip");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        async Task AddDirectoryToPackage(string path, Package package)
        {
            if (File.Exists(Path.Combine(path, NoBackupIndicatorFile)))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly))
            {
                var relativePath = file.Replace(_storageService.DataPath, string.Empty, StringComparison.Ordinal);

                var partUri = PackUriHelper.CreatePartUri(new Uri(relativePath, UriKind.Relative));

                var part = package.CreatePart(partUri, MediaTypeNames.Application.Octet, CompressionOption.NotCompressed);
                using (var fileStream = File.OpenRead(file))
                using (var partStream = part.GetStream())
                {
                    await fileStream.CopyToAsync(partStream).ConfigureAwait(false);
                }
            }

            foreach (var directory in Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly))
            {
                await AddDirectoryToPackage(directory, package).ConfigureAwait(false);
            }
        }
    }
}
