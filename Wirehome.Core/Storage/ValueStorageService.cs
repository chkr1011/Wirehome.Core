using Microsoft.Extensions.Logging;
using System;
using Wirehome.Core.Contracts;

namespace Wirehome.Core.Storage
{
    public sealed class ValueStorageService : WirehomeCoreService
    {
        const string ValueStorageDirectoryName = "ValueStorage";

        readonly object _syncRoot = new object();
        readonly StorageService _storageService;
        readonly ILogger<ValueStorageService> _logger;

        public ValueStorageService(StorageService storageService, ILogger<ValueStorageService> logger)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Write(RelativeValueStoragePath relativePath, object value)
        {
            if (relativePath is null) throw new ArgumentNullException(nameof(relativePath));

            var path = BuildPath(relativePath);
            lock (_syncRoot)
            {
                _storageService.WriteSerializedValue(value, path);
            }

            _logger.LogTrace($"Value '{relativePath}' written ({value}).");
        }

        public bool TryRead<TValue>(RelativeValueStoragePath relativePath, out TValue value)
        {
            if (relativePath is null) throw new ArgumentNullException(nameof(relativePath));

            var path = BuildPath(relativePath);
            lock (_syncRoot)
            {
                if (_storageService.TryReadSerializedValue(out value, path))
                {
                    _logger.LogTrace($"Value '{relativePath}' read ({value}).");
                    return true;
                }
            }
            
            _logger.LogTrace($"Value '{relativePath}' not found.");
            return false;
        }

        public void Delete(RelativeValueStoragePath relativePath)
        {
            if (relativePath is null) throw new ArgumentNullException(nameof(relativePath));

            var path = BuildPath(relativePath);
            lock (_syncRoot)
            {
                _storageService.DeletePath(path);
            }
            
            _logger.LogTrace($"Value '{relativePath}' deleted.");
        }

        static string BuildPath(RelativeValueStoragePath relativePath)
        {
            var path = global::System.IO.Path.Combine(relativePath.Segments.ToArray());
            return global::System.IO.Path.Combine(ValueStorageDirectoryName, path);
        }
    }

}
