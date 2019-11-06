using IronPython.Runtime;
using System;
using Wirehome.Core.Model;
using Wirehome.Core.Python;

namespace Wirehome.Core.Storage
{
    public class ValueStoragePythonProxy : IInjectedPythonProxy
    {
        private readonly ValueStorageService _valueStorageService;

        public ValueStoragePythonProxy(ValueStorageService valueStorageService)
        {
            _valueStorageService = valueStorageService ?? throw new ArgumentNullException(nameof(valueStorageService));
        }

        public string ModuleName => "value_storage";

        public void write(string container, string key, object value)
        {
            _valueStorageService.Write(container, key, value);
        }

        public object read(string container, string key, object defaultValue)
        {
            if (_valueStorageService.TryRead<object>(container, key, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public PythonDictionary read_object(string container, string key, PythonDictionary defaultValue)
        {
            if (_valueStorageService.TryRead<WirehomeDictionary>(container, key, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public string read_string(string container, string key, string defaultValue)
        {
            if (_valueStorageService.TryRead<string>(container, key, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public int read_int(string container, string key, int defaultValue)
        {
            if (_valueStorageService.TryRead<int>(container, key, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public float read_float(string container, string key, float defaultValue)
        {
            if (_valueStorageService.TryRead<float>(container, key, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public bool read_bool(string container, string key, bool defaultValue)
        {
            if (_valueStorageService.TryRead<bool>(container, key, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public void delete(string container, string key)
        {
            _valueStorageService.Delete(container, key);
        }

        public void delete(string container)
        {
            _valueStorageService.Delete(container);
        }
    }
}
