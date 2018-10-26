using System;
using IronPython.Runtime;
using Wirehome.Core.HTTP;

#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace Wirehome.Core.Python.Proxies
{
    public class HttpServerPythonProxy : IPythonProxy
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
    }
}
