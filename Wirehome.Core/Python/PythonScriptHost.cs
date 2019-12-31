using IronPython.Runtime;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using Wirehome.Core.Python.Exceptions;

namespace Wirehome.Core.Python
{
    public class PythonScriptHost
    {
        private readonly ScriptScope _scriptScope;
        private readonly IDictionary<string, object> _wirehomeWrapper;
        
        public PythonScriptHost(ScriptScope scriptScope, IDictionary<string, object> wirehomeWrapper)
        {
            _scriptScope = scriptScope ?? throw new ArgumentNullException(nameof(scriptScope));
            _wirehomeWrapper = wirehomeWrapper ?? throw new ArgumentNullException(nameof(wirehomeWrapper));
        }

        public void AddToWirehomeWrapper(string name, object value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            _wirehomeWrapper.Add(name, value);
        }

        public void Compile(string scriptCode)
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

            var pythonValue = PythonConvert.ToPython(value);
            lock (_scriptScope)
            {
                _scriptScope.SetVariable(name, pythonValue);
            }
        }

        public object GetVariable(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            object pythonValue;
            lock (_scriptScope)
            {
                pythonValue = _scriptScope.GetVariable(name);
            }

            return PythonConvert.FromPython(pythonValue);
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

            object result;
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
                    if (parameters?.Any() == false)
                    {
                        result = _scriptScope.Engine.Operations.Invoke(function);
                    }
                    else
                    {
                        var pythonParameters = parameters.Select(PythonConvert.ToPython).ToArray();
                        result = _scriptScope.Engine.Operations.Invoke(function, pythonParameters);
                    }                    
                }
                catch (Exception exception)
                {
                    var details = _scriptScope.Engine.GetService<ExceptionOperations>().FormatException(exception);
                    var message = "Error while invoking function. " + Environment.NewLine + details;

                    throw new PythonProxyException(message, exception);
                }
            }

            return PythonConvert.FromPython(result);
        }
    }
}
