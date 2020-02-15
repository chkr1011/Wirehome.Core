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
        readonly object[] _emptyParameters = new object[0];

        readonly object _syncRoot = new object();
        readonly Dictionary<string, PythonFunction> _functionsCache = new Dictionary<string, PythonFunction>();

        readonly ScriptScope _scriptScope;
        readonly IDictionary<string, object> _wirehomeWrapper;

        ObjectOperations _operations;

        public PythonScriptHost(ScriptScope scriptScope, IDictionary<string, object> wirehomeWrapper)
        {
            _scriptScope = scriptScope ?? throw new ArgumentNullException(nameof(scriptScope));
            _wirehomeWrapper = wirehomeWrapper ?? throw new ArgumentNullException(nameof(wirehomeWrapper));
        }

        public void AddToWirehomeWrapper(string name, object value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            lock (_syncRoot)
            {
                _wirehomeWrapper.Add(name, value);
            }
        }

        public void Compile(string scriptCode)
        {
            if (scriptCode == null) throw new ArgumentNullException(nameof(scriptCode));

            lock (_syncRoot)
            {
                try
                {
                    var source = _scriptScope.Engine.CreateScriptSourceFromString(scriptCode, SourceCodeKind.File);
                    var compiledCode = source.Compile();
                    compiledCode.Execute(_scriptScope);

                    _operations = compiledCode.Engine.CreateOperations(_scriptScope);

                    CreatePythonFunctionCache();
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
            lock (_syncRoot)
            {
                _scriptScope.SetVariable(name, pythonValue);
            }
        }

        public object GetVariable(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            object pythonValue;
            lock (_syncRoot)
            {
                pythonValue = _scriptScope.GetVariable(name);
            }

            return PythonConvert.FromPython(pythonValue);
        }

        public bool FunctionExists(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            lock (_syncRoot)
            {
                return _functionsCache.TryGetValue(name, out _);
            }
        }

        public object InvokeFunction(string name, params object[] parameters)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            object result;
            lock (_syncRoot)
            {
                try
                {
                    if (!_functionsCache.TryGetValue(name, out var pythonFunction))
                    {
                        throw new PythonProxyException($"Python function '{name}' not found.");
                    }

                    var pythonParameters = parameters?.Select(PythonConvert.ToPython).ToArray() ?? _emptyParameters;
                    result = _operations.Invoke(pythonFunction, pythonParameters);
                }
                catch (Exception exception)
                {
                    var details = _scriptScope.Engine.GetService<ExceptionOperations>().FormatException(exception);
                    var message = $"Error while Python invoking function '{name}'. " + Environment.NewLine + details;

                    throw new PythonProxyException(message, exception);
                }
            }

            return result;
        }

        void CreatePythonFunctionCache()
        {
            foreach (var memberName in _operations.GetMemberNames(_scriptScope))
            {
                if (!_operations.TryGetMember(_scriptScope, memberName, out var member))
                {
                    continue;
                }

                if (member is PythonFunction pythonFunction)
                {
                    _functionsCache.Add(memberName, pythonFunction);
                }
            }
        }
    }
}
