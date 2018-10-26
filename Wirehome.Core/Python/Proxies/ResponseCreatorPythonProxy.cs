#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using IronPython.Runtime;

namespace Wirehome.Core.Python.Proxies
{
    public class ResponseCreatorPythonProxy : IPythonProxy
    {
        public string ModuleName { get; } = "response_creator";

        public PythonDictionary success()
        {
            return new PythonDictionary
            {
                ["type"] = "success"
            };
        }

        public PythonDictionary exception(string message = null)
        {
            return new PythonDictionary
            {
                ["type"] = "exception",
                ["message"] = message
            };
        }

        public PythonDictionary parameter_missing(string parameter_name)
        {
            return new PythonDictionary
            {
                ["type"] = "exception.parameter_missing",
                ["parameter_name"] = parameter_name
            };
        }

        public PythonDictionary parameter_invalid(string parameter_name, string parameter_value = null, string message = null)
        {
            return new PythonDictionary
            {
                ["type"] = "exception.parameter_invalid",
                ["parameter_name"] = parameter_name,
                ["parameter_value"] = parameter_value,
                ["message"] = message
            };
        }
    }
}
