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

        private readonly string _binPath;
        private readonly string _dataPath;

        private readonly ILogger _logger;

        public StorageService(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<StorageService>();

            _binPath = AppDomain.CurrentDomain.BaseDirectory;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _dataPath = Path.Combine(Environment.ExpandEnvironmentVariables("%appData%"), "Wirehome");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _dataPath = Path.Combine("/etc/wirehome");
            }
            else
            {
                throw new NotSupportedException();
            }

            if (!Directory.Exists(_dataPath))
            {
                Directory.CreateDirectory(_dataPath);
            }

            _logger.Log(LogLevel.Information, $"Bin path  = {BinPath}");
            _logger.Log(LogLevel.Information, $"Data path = {DataPath}");
        }

        public string BinPath => _binPath;

        public string DataPath => _dataPath;

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

        public void Delete(params string[] path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var filename = Path.Combine(DataPath, Path.Combine(path));
            if (!filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                filename += ".json";
            }

            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
        }
    }
}
