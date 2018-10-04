using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Wirehome.Core.Storage
{
    public class StorageService
    {
        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            DateParseHandling = DateParseHandling.None,
            Formatting = Formatting.Indented
        };

        private readonly ILogger _logger;

        public StorageService(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<StorageService>();

            BinPath = AppDomain.CurrentDomain.BaseDirectory;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                DataPath = Path.Combine(Environment.ExpandEnvironmentVariables("%appData%"), "Wirehome");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                DataPath = Path.Combine("/etc/wirehome");
            }
            else
            {
                throw new NotSupportedException();
            }

            if (!Directory.Exists(DataPath))
            {
                Directory.CreateDirectory(DataPath);
            }

            _logger.Log(LogLevel.Information, $"Bin path  = {BinPath}");
            _logger.Log(LogLevel.Information, $"Data path = {DataPath}");
        }

        public string BinPath { get; }

        public string DataPath { get; }

        public List<string> EnumeratureDirectories(string pattern, params string[] path)
        {
            if (pattern == null) throw new ArgumentNullException(nameof(pattern));
            if (path == null) throw new ArgumentNullException(nameof(path));

            var directory = Path.Combine(DataPath, Path.Combine(path));
            if (!Directory.Exists(directory))
            {
                return new List<string>();
            }

            var directories = Directory.EnumerateDirectories(directory, pattern, SearchOption.AllDirectories).ToList();
            for (var i = 0; i < directories.Count; i++)
            {
                directories[i] = directories[i].Replace(directory, string.Empty).TrimStart(Path.DirectorySeparatorChar);
            }

            return directories;
        }

        public List<string> EnumerateFiles(string pattern, params string[] path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var relativePath = Path.Combine(path);
            var directory = Path.Combine(DataPath, relativePath);

            if (!Directory.Exists(directory))
            {
                return new List<string>();
            }

            var files = Directory.GetFiles(directory, pattern, SearchOption.AllDirectories).ToList();
            for (var i = 0; i < files.Count; i++)
            {
                files[i] = files[i].Replace(directory, string.Empty).TrimStart(Path.DirectorySeparatorChar);
            }

            return files;
        }

        public bool TryRead<TContent>(out TContent content, params string[] path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var filename = Path.Combine(DataPath, Path.Combine(path));
            if (!filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                filename += ".json";
            }

            if (!File.Exists(filename))
            {
                content = default(TContent);
                return false;
            }

            var json = File.ReadAllText(filename, Encoding.UTF8);
            content = JsonConvert.DeserializeObject<TContent>(json, _jsonSerializerSettings);
            return true;
        }

        public bool TryReadText(out string content, params string[] path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var filename = Path.Combine(DataPath, Path.Combine(path));
            if (!File.Exists(filename))
            {
                content = null;
                return false;
            }

            content = File.ReadAllText(filename, Encoding.UTF8);
            return true;
        }
        
        public bool TryReadRaw(out byte[] content, params string[] path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var filename = Path.Combine(DataPath, Path.Combine(path));
            if (!File.Exists(filename))
            {
                content = null;
                return false;
            }

            content = File.ReadAllBytes(filename);
            return true;
        }

        public void Write(object content, params string[] path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var filename = Path.Combine(DataPath, Path.Combine(path));
            if (!filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                filename += ".json";
            }

            var directory = Path.GetDirectoryName(filename);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (content == null)
            {
                File.WriteAllBytes(filename, new byte[0]);
                return;
            }

            var json = JsonConvert.SerializeObject(content, _jsonSerializerSettings);
            File.WriteAllText(filename, json, Encoding.UTF8);
        }

        public void WriteRaw(byte[] content, params string[] path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var filename = Path.Combine(DataPath, Path.Combine(path));
            var directory = Path.GetDirectoryName(filename);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(filename, content ?? new byte[0]);
        }

        public void WriteText(string content, params string[] path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var filename = Path.Combine(DataPath, Path.Combine(path));
            var directory = Path.GetDirectoryName(filename);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filename, content ?? string.Empty, Encoding.UTF8);
        }

        public void DeleteFile(params string[] path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var fullPath = Path.Combine(DataPath, Path.Combine(path));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        public void DeleteDirectory(params string[] path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var fullPath = Path.Combine(DataPath, Path.Combine(path));
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
            }
        }
    }
}
