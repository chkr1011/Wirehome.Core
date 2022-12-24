#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using System.Diagnostics;
using System.Text;
using IronPython.Runtime;
using Microsoft.Scripting.Utils;
using Wirehome.Core.Python.Models;

namespace Wirehome.Core.Python.Proxies.OS;

public class OSPythonProxy : IInjectedPythonProxy
{
    public string ModuleName { get; } = "os";

    public static PythonDictionary execute(PythonDictionary parameters)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        try
        {
            var launchParameters = ParseLaunchParameters(parameters);
            var result = new ExecuteResult();

            using (var process = StartProcess(launchParameters))
            {
                var hasExited = process.WaitForExit(launchParameters.Timeout);
                if (!hasExited)
                {
                    process.Kill();
                }

                result.OutputData = process.StandardOutput.ReadToEnd();
                result.ErrorData = process.StandardError.ReadToEnd();

                result.ExitCode = process.ExitCode;
            }

            return ConvertToPythonDictionary(result);
        }
        catch (Exception exception)
        {
            return PythonConvert.ToPythonDictionary(new ExceptionPythonModel(exception).ToDictionary());
        }
    }

    public static PythonDictionary launch(PythonDictionary parameters)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        try
        {
            var launchParameters = ParseLaunchParameters(parameters);
            var result = new LaunchResult();

            using (var process = StartProcess(launchParameters))
            {
                result.Pid = process.Id;
            }

            return ConvertToPythonDictionary(result);
        }
        catch (Exception exception)
        {
            return PythonConvert.ToPythonDictionary(new ExceptionPythonModel(exception).ToDictionary());
        }
    }

    static PythonDictionary ConvertToPythonDictionary(LaunchResult launchResult)
    {
        return new PythonDictionary
        {
            ["pid"] = launchResult.Pid
        };
    }

    static PythonDictionary ConvertToPythonDictionary(ExecuteResult executeResult)
    {
        return new PythonDictionary
        {
            ["exit_code"] = executeResult.ExitCode,
            ["output_data"] = executeResult.OutputData,
            ["error_data"] = executeResult.ErrorData
        };
    }

    static LaunchParamters ParseLaunchParameters(PythonDictionary pythonDictionary)
    {
        var launchParameters = new LaunchParamters
        {
            FileName = Convert.ToString(pythonDictionary.get("file_name", null)),
            Timeout = Convert.ToInt32(pythonDictionary.get("timeout", 60000))
        };

        var arguments = pythonDictionary.get("arguments", null);

        if (arguments is string argumentsText)
        {
            launchParameters.Arguments.Add(argumentsText);
        }
        else if (arguments is List argumentsList)
        {
            foreach (var argument in argumentsList)
            {
                launchParameters.Arguments.Add(Convert.ToString(argument));
            }
        }

        return launchParameters;
    }

    static Process StartProcess(LaunchParamters parameters)
    {
        var startInfo = new ProcessStartInfo(parameters.FileName)
        {
            UseShellExecute = false, // Must be false to redirect output.
            CreateNoWindow = true,

            RedirectStandardOutput = true,
            StandardErrorEncoding = Encoding.UTF8,

            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        if (parameters.Arguments.Count > 1)
        {
            startInfo.ArgumentList.AddRange(parameters.Arguments);
        }
        else if (parameters.Arguments.Count == 1)
        {
            startInfo.Arguments = parameters.Arguments[0];
        }

        var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("No process was started.");
        }

        return process;
    }
}