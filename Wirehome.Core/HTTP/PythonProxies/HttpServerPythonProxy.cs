using IronPython.Runtime;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using System;
using System.Globalization;
using Wirehome.Core.Python;

#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace Wirehome.Core.HTTP.PythonProxies
{
    public class HttpServerPythonProxy : IInjectedPythonProxy
    {
        readonly HttpServerService _httpServerService;

        public HttpServerPythonProxy(HttpServerService httpServerService)
        {
            _httpServerService = httpServerService ?? throw new ArgumentNullException(nameof(httpServerService));
        }

        public delegate PythonDictionary RequestHandler(PythonDictionary request);

        public string ModuleName { get; } = "http_server";

        public string register_route(string uid, string uri_template, RequestHandler handler)
        {
            return _httpServerService.RegisterRoute(uid, uri_template, request => handler(PythonConvert.ToPythonDictionary(request)));
        }

        public void unregister_route(string uid)
        {
            _httpServerService.UnregisterRoute(uid);
        }

        public static PythonDictionary match_template(string template, string path)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (path == null) throw new ArgumentNullException(nameof(path));

            var routeTemplate = TemplateParser.Parse(template);
            var values = new RouteValueDictionary();
            var templateMatcher = new TemplateMatcher(routeTemplate, values);
            var isMatch = templateMatcher.TryMatch(path, values);

            var resultValues = new PythonDictionary();
            foreach (var value in values)
            {
                resultValues.Add(value.Key, Convert.ToString(value.Value, CultureInfo.InvariantCulture));
            }

            return new PythonDictionary
            {
                ["is_match"] = isMatch,
                ["values"] = resultValues
            };
        }
    }
}
