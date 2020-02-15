#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
using System.Collections.Generic;

namespace Wirehome.Core.Python.Proxies.OS
{
    public class LaunchParamters
    {
        public string FileName { get; set; }

        public List<string> Arguments { get; set; } = new List<string>();

        public int Timeout { get; set; } = 60000;
    }
}
