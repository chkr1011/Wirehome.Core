using System;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Proxies;
using Wirehome.Core.Storage;

namespace Wirehome.Core.HTTP
{
    public class HttpServerService
    {
        private readonly StorageService _storageService;
        private readonly ILogger _logger;

        public HttpServerService(StorageService storageService, PythonEngineService pythonEngineService, ILoggerFactory loggerFactory)
        {
            if (pythonEngineService == null) throw new ArgumentNullException(nameof(pythonEngineService));
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            _storageService = storageService;

            _logger = loggerFactory.CreateLogger<HttpServerService>();

            pythonEngineService.RegisterSingletonProxy(new HttpClientPythonProxy());
        }

        public void Start()
        {

        }
    }
}
