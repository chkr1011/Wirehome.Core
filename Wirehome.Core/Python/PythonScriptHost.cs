using System;
using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Wirehome.Core.Python.Exceptions;

namespace Wirehome.Core.Python
{
    public class PythonScriptHost
    {
        private readonly ScriptScope _scriptScope;
        private readonly ILogger _logger;

        public PythonScriptHost(ScriptScope scriptScope, ILoggerFactory loggerFactory)
        {
            _scriptScope = scriptScope ?? throw new ArgumentNullException(nameof(scriptScope));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<PythonScriptHost>();
        }

        public void Initialize(string scriptCode)
        {
            if (scriptCode == null) throw new ArgumentNullException(nameof(scriptCode));

            lock (_scriptScope)
            {
                try
                {
                    var source = _scriptScope.Engine.CreateScriptSourceFromString(scriptCode, SourceCodeKind.File);
                    var compiledCode = source.Compile();
                    compiledCode.Execute(_scriptScope);
                }
                catch (Exception exception)
                {
                    var details = _scriptScope.Engine.GetService<ExceptionOperations>().FormatException(exception);
                    var message = "Error while initializing Python script host." + Environment.NewLine + details;

                    throw new PythonProxyException(message, exception);
                }
            }
        }

        public void SetVariable(string name, object value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            lock (_scriptScope)
            {
                _scriptScope.SetVariable(name, PythonConvert.ToPython(value));
            }
        }

        public object GetVariable(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            lock (_scriptScope)
            {
                return PythonConvert.FromPython(_scriptScope.GetVariable(name));
            }
        }

        public bool FunctionExists(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            lock (_scriptScope)
            {
                if (!_scriptScope.Engine.Operations.TryGetMember(_scriptScope, name, out var member))
                {
                    return false;
                }

                if (!(member is PythonFunction))
                {
                    return false;
                }
            }

            return true;
        }

        public object InvokeFunction(string name, params object[] parameters)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            lock (_scriptScope)
            {
                if (!_scriptScope.Engine.Operations.TryGetMember(_scriptScope, name, out var member))
                {
                    throw new PythonProxyException($"Function '{name}' not found.");
                }

                if (!(member is PythonFunction function))
                {
                    throw new PythonProxyException($"Member '{name}' is no Python function.");
                }

                try
                {
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        parameters[i] = PythonConvert.ToPython(parameters[i]);
                    }

                    object result = _scriptScope.Engine.Operations.Invoke(function, parameters);
                    return PythonConvert.FromPython(result);
                }
                catch (Exception exception)
                {
                    var details = _scriptScope.Engine.GetService<ExceptionOperations>().FormatException(exception);
                    var message = "Error while invoking function. " + Environment.NewLine + details;

                    _logger.Log(LogLevel.Warning, exception, message);

                    throw new PythonProxyException(message, exception);
                }
            }
        }
    }
}
