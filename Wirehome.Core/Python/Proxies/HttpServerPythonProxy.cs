using System;
using Wirehome.Core.Model;

#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace Wirehome.Core.Python.Proxies
{
    public class HttpServerPythonProxy : IPythonProxy
    {
        public string ModuleName { get; } = "http_server";

        public string register_route(string uri_template, Func<WirehomeDictionary, WirehomeDictionary> handler)
        {
            return Guid.NewGuid().ToString();
        }

        public void unregister_route(string uid)
        {

        }
    }
}
