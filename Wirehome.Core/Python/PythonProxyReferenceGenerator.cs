using System;
using System.Linq;
using System.Reflection;
using System.Text;
using IronPython.Runtime;

namespace Wirehome.Core.Python;

public sealed class PythonProxyReferenceGenerator
{
    readonly PythonProxyFactory _pythonProxyFactory;

    public PythonProxyReferenceGenerator(PythonProxyFactory pythonProxyFactory)
    {
        _pythonProxyFactory = pythonProxyFactory ?? throw new ArgumentNullException(nameof(pythonProxyFactory));
    }

    public string Generate()
    {
        var output = new StringBuilder();

        output.AppendLine("# Wirehome.Core Python API");
        output.AppendLine($"> Version: {WirehomeCoreVersion.Version}");

        output.AppendLine("## Modules");

        var pythonProxies = _pythonProxyFactory.GetPythonProxies();
        foreach (var pythonProxy in pythonProxies.OrderBy(p => p.ModuleName))
        {
            GenerateModuleReferenceDocument(pythonProxy, output);
        }

        return output.ToString();
    }

    static string GenerateCallbackSignature(Type type)
    {
        var output = new StringBuilder();
        output.Append("callback(");

        var arguments = type.GetGenericArguments();
        if (arguments.Length > 0)
        {
            var argumentPosition = 1;
            foreach (var argument in arguments)
            {
                output.Append($"param{argumentPosition}, ");
                argumentPosition++;
            }

            output.Remove(output.Length - 2, 2);
        }

        output.Append(")");

        return output.ToString();
    }

    static void GenerateModuleReferenceDocument(IPythonProxy pythonProxy, StringBuilder output)
    {
        output.AppendLine($"### _{pythonProxy.ModuleName}_ module");

        output.AppendLine();

        var methods = pythonProxy.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(m => !m.IsSpecialName).OrderBy(m => m.Name);

        foreach (var method in methods)
        {
            output.AppendLine($"#### Method _{method.Name}_");

            output.AppendLine("Signature:");
            output.AppendLine();
            output.Append($"`wirehome.{pythonProxy.ModuleName}.{method.Name}(");

            var parameters = method.GetParameters();
            if (parameters.Length > 0)
            {
                foreach (var parameter in method.GetParameters())
                {
                    output.Append(parameter.Name + ", ");
                }

                output.Remove(output.Length - 2, 2);
            }

            output.AppendLine(")`");
            output.AppendLine();

            if (parameters.Length > 0)
            {
                output.AppendLine("Parameters:");
                foreach (var parameter in parameters)
                {
                    output.AppendLine($"* {parameter.Name} : `{GetPythonTypeName(parameter.ParameterType)}`");
                }

                output.AppendLine();
            }

            output.AppendLine("Return value: ");
            if (method.ReturnType == typeof(void))
            {
                output.AppendLine("_This method has no return value._");
            }
            else
            {
                output.AppendLine($"`{GetPythonTypeName(method.ReturnType)}`");
            }
        }

        output.AppendLine();
    }

    static string GetPythonTypeName(Type type)
    {
        if (type == typeof(string))
        {
            return "str";
        }

        if (type == typeof(int))
        {
            return "int";
        }

        if (type == typeof(uint))
        {
            return "unsigned int";
        }

        if (type == typeof(long))
        {
            return "long";
        }

        if (type == typeof(ulong))
        {
            return "unsigned long";
        }

        if (type == typeof(bool))
        {
            return "bool";
        }

        if (type == typeof(PythonDictionary))
        {
            return "dictionary";
        }

        if (type == typeof(PythonList))
        {
            return "list";
        }

        if (type == typeof(object))
        {
            return "Any";
        }

        if (type.BaseType == typeof(MulticastDelegate))
        {
            return GenerateCallbackSignature(type.BaseType);
        }

        return type.Name.ToLowerInvariant();
    }
}