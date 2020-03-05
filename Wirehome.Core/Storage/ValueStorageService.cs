using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using Wirehome.Core.Contracts;

namespace Wirehome.Core.Storage
{
    public class ValueStorageService : IService
    {
        const string ValueStorageDirectoryName = "ValueStorage";

        readonly StorageService _storageService;
        readonly ILogger<ValueStorageService> _logger;

        public ValueStorageService(StorageService storageService, ILogger<ValueStorageService> logger)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Start()
        {
        }

        public void Write(RelativeValueStoragePath relativePath, object value)
        {
            if (relativePath is null) throw new ArgumentNullException(nameof(relativePath));

            var path = BuildPath(relativePath);

            _storageService.Write(value, path);
            _logger.LogTrace($"Value '{relativePath}' written ({value}).");
        }

        public bool TryRead<TValue>(RelativeValueStoragePath relativePath, out TValue value)
        {
            if (relativePath is null) throw new ArgumentNullException(nameof(relativePath));

            var path = BuildPath(relativePath);

            if (_storageService.TryRead(out value, path))
            {
                _logger.LogTrace($"Value '{relativePath}' read ({value}).");
                return true;
            }

            _logger.LogTrace($"Value '{relativePath}' not found.");
            return false;
        }

        public void Delete(RelativeValueStoragePath relativePath)
        {
            if (relativePath is null) throw new ArgumentNullException(nameof(relativePath));

            var path = BuildPath(relativePath);

            _storageService.DeletePath(path);
            _logger.LogTrace($"Value '{relativePath}' deleted.");
        }

        string BuildPath(RelativeValueStoragePath relativePath)
        {
            if (relativePath.Paths.Any(p => p.Contains("/")) || relativePath.Paths.Any(p => p.Contains("\\")))
            {
                throw new InvalidOperationException("The path is invalid.");
            }

            var path = global::System.IO.Path.Combine(relativePath.Paths.ToArray());
            path = global::System.IO.Path.Combine(ValueStorageDirectoryName, path);

            return path;
        }
    }

}
