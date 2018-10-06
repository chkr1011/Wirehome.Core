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

        public string register_route(string uid, string uri_template, Func<WirehomeDictionary, WirehomeDictionary> handler)
        {
            if (string.IsNullOrEmpty(uid))
            {
                uid = Guid.NewGuid().ToString("D");
            }

            return uid;
        }

        public void unregister_route(string uid)
        {

        }
    }
}
