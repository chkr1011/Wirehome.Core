using System;
using System.Globalization;
using IronPython.Runtime;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Proxies;

#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace Wirehome.Core.HTTP
{
    public class HttpServerPythonProxy : IInjectedPythonProxy
    {
        private readonly HttpServerService _httpServerService;

        public HttpServerPythonProxy(HttpServerService httpServerService)
        {
            _httpServerService = httpServerService ?? throw new ArgumentNullException(nameof(httpServerService));
        }

        public string ModuleName { get; } = "http_server";

        public string register_route(string uid, string uri_template, Func<PythonDictionary, PythonDictionary> handler)
        {
            return _httpServerService.RegisterRoute(uid, uri_template, request => handler(request));
        }

        public void unregister_route(string uid)
        {
            _httpServerService.UnregisterRoute(uid);
        }

        public PythonDictionary match_template(string template, string path)
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
