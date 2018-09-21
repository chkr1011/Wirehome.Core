using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Proxies;

namespace Wirehome.Core.Diagnostics
{
    public class SystemStatusService
    {
        private readonly ConcurrentDictionary<string, Func<object>> _values = new ConcurrentDictionary<string, Func<object>>();

        public SystemStatusService(PythonEngineService pythonEngineService)
        {
            if (pythonEngineService == null) throw new ArgumentNullException(nameof(pythonEngineService));
            
            pythonEngineService.RegisterSingletonProxy(new SystemInformationPythonProxy(this));
        }

        public void Set(string uid, object value)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _values[uid] = () => value;
        }

        public void Set(string uid, Func<object> value)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _values[uid] = value ?? throw new ArgumentNullException(nameof(value));
        }

        public object Get(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            if (!_values.TryGetValue(uid, out var valueProvider))
            {
                return null;
            }

            return valueProvider();
        }

        public void Delete(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _values.TryRemove(uid, out _);
        }

        public Dictionary<string, object> All()
        {
            var result = new Dictionary<string, object>();
            foreach (var value in _values)
            {
                result[value.Key] = value.Value();
            }

            return result;
        }
    }
}
