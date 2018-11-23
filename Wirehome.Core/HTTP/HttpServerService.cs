using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Contracts;
using Wirehome.Core.Model;
using Wirehome.Core.Storage;

namespace Wirehome.Core.HTTP
{
    public class HttpServerService : IService
    {
        private readonly ConcurrentDictionary<string, HttpRequestInterceptor> _interceptors = new ConcurrentDictionary<string, HttpRequestInterceptor>();

        private readonly JsonSerializerService _jsonSerializerService;
        private readonly ILogger _logger;

        public HttpServerService(JsonSerializerService jsonSerializerService, ILogger<HttpServerService> logger)
        {
            _jsonSerializerService = jsonSerializerService ?? throw new ArgumentNullException(nameof(jsonSerializerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Start()
        {
        }

        public string RegisterRoute(string uid, string uriTemplate, Func<WirehomeDictionary, WirehomeDictionary> handler)
        {
            if (uriTemplate == null) throw new ArgumentNullException(nameof(uriTemplate));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            if (string.IsNullOrEmpty(uid))
            {
                uid = Guid.NewGuid().ToString("D");
            }

            var interceptor = new HttpRequestInterceptor(uriTemplate, handler, _jsonSerializerService, _logger);
            _interceptors[uid] = interceptor;

            return uid;
        }

        public void UnregisterRoute(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _interceptors.TryRemove(uid, out _);
        }

        public Task HandleRequestAsync(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            foreach (var interceptor in _interceptors)
            {
                if (interceptor.Value.HandleRequest(context))
                {
                    return Task.CompletedTask;
                }
            }

            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return Task.CompletedTask;
        }
    }
}
