#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using IronPython.Runtime;
using System.Collections.Generic;
using Wirehome.Core.Foundation.Model;

namespace Wirehome.Core.Python.Proxies.OS
{
    public class LaunchParamters : TypedWirehomeDictionary
    {
        public static implicit operator LaunchParamters(PythonDictionary pythonDictionary)
        {
            return PythonConvert.CreateModel<LaunchParamters>(pythonDictionary);
        }

        public string FileName { get; set; }

        public List<string> Arguments { get; set; } = new List<string>();

        public int Timeout { get; set; } = 60000;
    }
}
