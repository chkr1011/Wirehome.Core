using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Model;
using Wirehome.Core.Storage;

namespace Wirehome.Core.HTTP
{
    public class HttpRequestInterceptor
    {
        private readonly Func<WirehomeDictionary, WirehomeDictionary> _handler;
        private readonly JsonSerializerService _jsonSerializerService;
        private readonly ILogger _logger;
        private readonly RouteTemplate _routeTemplate;

        public HttpRequestInterceptor(string uriTemplate, Func<WirehomeDictionary, WirehomeDictionary> handler, JsonSerializerService jsonSerializerService, ILogger logger)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _jsonSerializerService = jsonSerializerService ?? throw new ArgumentNullException(nameof(jsonSerializerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(_logger));

            if (uriTemplate == null) throw new ArgumentNullException(nameof(uriTemplate));
            _routeTemplate = TemplateParser.Parse(uriTemplate);
        }

        public bool HandleRequest(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var values = new RouteValueDictionary();
            var templateMatcher = new TemplateMatcher(_routeTemplate, values);
            var isMatch = templateMatcher.TryMatch(context.Request.Path, values);

            if (!isMatch)
            {
                return false;
            }

            var converter = new HttpRequestConverter(_jsonSerializerService);
            var arguments = converter.WrapContext(context);

            try
            {
                var result = _handler(arguments) ?? new WirehomeDictionary();
                converter.UnwrapContext(result, context);
                return true;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while intercepting HTTP request.");
            }

            return false;
        }
    }
}