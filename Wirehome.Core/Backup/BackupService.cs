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
        private const string BackupsDirectory = "Backups";

        private readonly StorageService _storageService;
        
        public BackupService(StorageService storageService)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        }

        public void Start()
        {
        }

        public async Task CreateBackupAsync()
        {
            var now = DateTime.Now;

            var tempFilename = Path.GetTempFileName();
            
            using (var zip = Package.Open(tempFilename, FileMode.OpenOrCreate))
            {
                zip.PackageProperties.Title = "Wirehome.Backup";
                zip.PackageProperties.Creator = "Wirehome.Core";
                zip.PackageProperties.Description = "Backup package for Wirehome.Core data.";
                zip.PackageProperties.Created = now;
                
                foreach (var file in Directory.GetFiles(_storageService.DataPath, "*.*", SearchOption.AllDirectories))
                {
                    var relativePath = file.Replace(_storageService.DataPath, ".");

                    var partUri = PackUriHelper.CreatePartUri(new Uri(relativePath, UriKind.Relative));

                    var part = zip.CreatePart(partUri, MediaTypeNames.Application.Octet, CompressionOption.Normal);
                    using (var fileStream = File.OpenRead(file))
                    using (var partStream = part.GetStream())
                    {
                        await fileStream.CopyToAsync(partStream).ConfigureAwait(false);
                    }
                }
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
                uids.Add(Path.GetFileName(file.Replace(".zip", string.Empty)));
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
    }
}
