#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using IronPython.Runtime;

namespace Wirehome.Core.Python.Proxies;

public sealed class ResponseCreatorPythonProxy : IInjectedPythonProxy
{
    public string ModuleName { get; } = "response_creator";

    public static PythonDictionary disabled()
    {
        return new PythonDictionary
        {
            ["type"] = "exception.disabled"
        };
    }

    public static PythonDictionary exception(string message = null)
    {
        return new PythonDictionary
        {
            ["type"] = "exception",
            ["message"] = message
        };
    }

    public static PythonDictionary not_supported(string origin_type)
    {
        return new PythonDictionary
        {
            ["type"] = "exception.not_supported",
            ["origin_type"] = origin_type
        };
    }

    public static PythonDictionary parameter_invalid(string parameter_name, string parameter_value = null, string message = null)
    {
        return new PythonDictionary
        {
            ["type"] = "exception.parameter_invalid",
            ["parameter_name"] = parameter_name,
            ["parameter_value"] = parameter_value,
            ["message"] = message
        };
    }

    public static PythonDictionary parameter_missing(string parameter_name)
    {
        return new PythonDictionary
        {
            ["type"] = "exception.parameter_missing",
            ["parameter_name"] = parameter_name
        };
    }

    public static PythonDictionary success()
    {
        return new PythonDictionary
        {
            ["type"] = "success"
        };
    }
}