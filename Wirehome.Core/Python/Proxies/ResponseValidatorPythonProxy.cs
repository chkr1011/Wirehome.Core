#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using IronPython.Runtime;
using System;
using System.Globalization;
using Wirehome.Core.Constants;

namespace Wirehome.Core.Python.Proxies
{
    public class ResponseValidatorPythonProxy : IInjectedPythonProxy
    {
        public string ModuleName { get; } = "response_validator";

        public static bool is_success(PythonDictionary response)
        {
            if (response == null)
            {
                return false;
            }

            if (!response.TryGetValue("type", out var value))
            {
                return false;
            }

            var type = Convert.ToString(value, CultureInfo.InvariantCulture);

            return string.Equals(type, ControlType.Success, StringComparison.Ordinal);
        }
    }
}
