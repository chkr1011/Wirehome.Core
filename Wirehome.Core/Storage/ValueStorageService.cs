using Microsoft.Extensions.Logging;
using System;
using Wirehome.Core.Contracts;

namespace Wirehome.Core.Storage
{
    public class ValueStorageService : IService
    {
        private const string ValueStorageDirectoryName = "ValueStorage";

        private readonly StorageService _storageService;
        private readonly ILogger<ValueStorageService> _logger;

        public ValueStorageService(StorageService storageService, ILogger<ValueStorageService> logger)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Start()
        {
        }

        public void Write(string container, string key, object value)
        {
            if (container is null) throw new ArgumentNullException(nameof(container));
            if (key is null) throw new ArgumentNullException(nameof(key));

            _storageService.Write(value, ValueStorageDirectoryName, container, key);
            _logger.LogTrace($"Value '{container}/{key}' written ({value}).");
        }

        public bool TryRead<TValue>(string container, string key, out TValue value)
        {
            if (container is null) throw new ArgumentNullException(nameof(container));
            if (key is null) throw new ArgumentNullException(nameof(key));

            if (_storageService.TryRead(out value, ValueStorageDirectoryName, container, key))
            {
                _logger.LogTrace($"Value '{container}/{key}' read ({value}).");
                return true;
            }

            _logger.LogTrace($"Value '{container}/{key}' not found.");
            return false;
        }

        public void Delete(string container)
        {
            if (container is null) throw new ArgumentNullException(nameof(container));

            _storageService.DeleteFile(ValueStorageDirectoryName, container);
            _logger.LogTrace($"Value container '{container}' deleted.");
        }

        public void Delete(string container, string key)
        {
            if (container is null) throw new ArgumentNullException(nameof(container));
            if (key is null) throw new ArgumentNullException(nameof(key));

            _storageService.DeleteFile(ValueStorageDirectoryName, container, key);
            _logger.LogTrace($"Value '{container}/{key}' deleted.");
        }
    }
    
}
