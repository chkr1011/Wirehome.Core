using IronPython.Runtime;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Wirehome.Core.Python.Exceptions;

namespace Wirehome.Core.Python
{
    //public class PythonScriptHostStorage : IDictionary<string, object>
    //{
    //    readonly Dictionary<string, object> _storage = new Dictionary<string, object>();

    //    public object this[string key]
    //    {
    //        get
    //        {
    //            Debug.WriteLine("Python.this get " + key);

    //            lock (_storage)
    //            {
    //                return _storage[key];
    //            }
    //        }

    //        set
    //        {
    //            Debug.WriteLine("Python.this set " + key);

    //            lock (_storage)
    //            {
    //                _storage[key] = value;
    //            }
    //        }
    //    }

    //    public ICollection<string> Keys
    //    {
    //        get
    //        {
    //            lock (_storage)
    //            {
    //                return _storage.Keys;
    //            }
    //        }
    //    }

    //    public ICollection<object> Values => throw new NotImplementedException();

    //    public int Count => throw new NotImplementedException();

    //    public bool IsReadOnly => throw new NotImplementedException();

    //    public void Add(string key, object value)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void Add(KeyValuePair<string, object> item)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void Clear()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public bool Contains(KeyValuePair<string, object> item)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public bool ContainsKey(string key)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public bool Remove(string key)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public bool Remove(KeyValuePair<string, object> item)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value)
    //    {
    //        Debug.WriteLine("Python.TryGetValue " + key);

    //        lock (_storage)
    //        {
    //            return _storage.TryGetValue(key, out value);
    //        }
    //    }

    //    IEnumerator IEnumerable.GetEnumerator()
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    public class PythonScriptHost
    {
        readonly object[] _emptyParameters = Array.Empty<object>();
        readonly object _syncRoot = new object();
        //readonly PythonScriptHostStorage _storage = new PythonScriptHostStorage();
        readonly IDictionary<string, object> _wirehomeWrapper = new ExpandoObject();
        readonly Dictionary<string, PythonFunction> _functionsCache = new Dictionary<string, PythonFunction>();

        readonly ScriptEngine _scriptEngine;
        readonly List<IPythonProxy> _pythonProxies;

        ScriptScope _scriptScope;
        ObjectOperations _operations;

        public PythonScriptHost(ScriptEngine scriptEngine, List<IPythonProxy> pythonProxies)
        {
            _scriptEngine = scriptEngine ?? throw new ArgumentNullException(nameof(scriptEngine));
            _pythonProxies = pythonProxies ?? throw new ArgumentNullException(nameof(pythonProxies));

            _scriptScope = _scriptEngine.CreateScope();
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
                    var source = _scriptEngine.CreateScriptSourceFromString(scriptCode, SourceCodeKind.File);
                    var compiledCode = source.Compile();
                    compiledCode.Execute(_scriptScope);
                    _operations = _scriptEngine.CreateOperations(_scriptScope);

                    foreach (var pythonProxy in _pythonProxies)
                    {
                        _wirehomeWrapper.Add(pythonProxy.ModuleName, pythonProxy);
                    }

                    _scriptScope.SetVariable("wirehome", _wirehomeWrapper);

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
