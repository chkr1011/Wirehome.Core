using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Wirehome.Core.Storage;

namespace Wirehome.Core.HTTP.PythonProxies
{
    public class HttpRequestInterceptor
    {
        readonly Func<IDictionary<object, object>, IDictionary<object, object>> _handler;
        readonly JsonSerializerService _jsonSerializerService;
        readonly ILogger _logger;
        readonly RouteTemplate _routeTemplate;

        public HttpRequestInterceptor(string uriTemplate, Func<IDictionary<object, object>, IDictionary<object, object>> handler, JsonSerializerService jsonSerializerService, ILogger logger)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _jsonSerializerService = jsonSerializerService ?? throw new ArgumentNullException(nameof(jsonSerializerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
                return false;

            var httpRequestConverter = new HttpRequestConverter(_jsonSerializerService);
            var arguments = httpRequestConverter.WrapContext(context);

            try
            {
                var result = _handler(arguments) ?? new Dictionary<object, object>();
                httpRequestConverter.UnwrapContext(result, context);
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