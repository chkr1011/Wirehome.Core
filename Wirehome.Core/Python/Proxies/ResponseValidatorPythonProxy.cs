using System;
using System.Globalization;
using IronPython.Runtime;
using Wirehome.Core.Constants;

#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace Wirehome.Core.Python.Proxies
{
    public class ResponseValidatorPythonProxy : IPythonProxy
    {
        public string ModuleName { get; } = "response_validator";

        public bool is_success(PythonDictionary response)
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
