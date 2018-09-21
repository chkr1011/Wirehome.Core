using System;
using System.Collections;
using System.Collections.Generic;
using IronPython.Runtime;
using Newtonsoft.Json.Linq;
using Wirehome.Core.Model;

namespace Wirehome.Core.Python
{
    public static class PythonConvert
    {
        public static object ConvertFromPython(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is PythonDictionary pythonDictionary)
            {
                return ConvertFromPython(pythonDictionary);
            }

            if (value is List pythonList)
            {
                var list = new List<object>();
                foreach (var item in pythonList)
                {
                    list.Add(ConvertFromPython(item));
                }

                return list;
            }

            return value;
        }

        public static object ConvertForPython(object value)
        {
            if (value is null || value is string || value is bool || value is int || value is float || value is long || value is double)
            {
                return value;
            }

            if (value is WirehomeDictionary wirehomeDictionary)
            {
                var pythonDictionary = new PythonDictionary();
                foreach (var entry in wirehomeDictionary)
                {
                    pythonDictionary.Add(entry.Key, ConvertForPython(entry.Value));
                }

                return pythonDictionary;
            }

            if (value is IDictionary dictionary)
            {
                var pythonDictionary = new PythonDictionary();
                foreach (var entryKey in dictionary.Keys)
                {
                    pythonDictionary.Add(entryKey, ConvertForPython(dictionary[entryKey]));
                }

                return pythonDictionary;
            }

            if (value is JArray array)
            {
                var result = new List(); // This is a python list.
                foreach (var item in array)
                {
                    result.Add(ConvertForPython(item));
                }

                return result;
            }

            if (value is JObject @object)
            {
                var result = new PythonDictionary();
                foreach (var property in @object.Properties())
                {
                    result.Add(property.Name, ConvertForPython(property.Value));
                }

                return result;
            }

            if (value is JValue jValue)
            {
                if (jValue.Type == JTokenType.Null)
                {
                    return null;
                }

                return jValue.ToObject<object>();
            }

            if (value is IEnumerable items)
            {
                var result = new List(); // This is a python list.
                foreach (var item in items)
                {
                    result.Add(ConvertForPython(item));
                }

                return result;
            }
            
            return value;
        }

        private static object ConvertFromPython(PythonDictionary pythonDictionary)
        {
            if (pythonDictionary == null) throw new ArgumentNullException(nameof(pythonDictionary));

            var wirehomeDictionary = new WirehomeDictionary();
            foreach (var entry in pythonDictionary)
            {
                var key = Convert.ToString(entry.Key);
                wirehomeDictionary.TryAdd(key, ConvertFromPython(entry.Value));
            }

            return wirehomeDictionary;
        }
    }
}
