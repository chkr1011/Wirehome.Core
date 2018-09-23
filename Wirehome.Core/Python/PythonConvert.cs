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
        public static List ToPythonList(IEnumerable items)
        {
            if (items == null)
            {
                return null;
            }

            var list = new List();
            foreach (var item in items)
            {
                list.Add(ForPython(item));
            }

            return list;
        }

        public static object FromPython(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is PythonDictionary pythonDictionary)
            {
                return FromPython(pythonDictionary);
            }

            if (value is List pythonList)
            {
                var list = new List<object>();
                foreach (var item in pythonList)
                {
                    list.Add(FromPython(item));
                }

                return list;
            }

            return value;
        }

        public static object ForPython(object value)
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
                    pythonDictionary.Add(entry.Key, ForPython(entry.Value));
                }

                return pythonDictionary;
            }

            if (value is IDictionary dictionary)
            {
                var pythonDictionary = new PythonDictionary();
                foreach (var entryKey in dictionary.Keys)
                {
                    pythonDictionary.Add(entryKey, ForPython(dictionary[entryKey]));
                }

                return pythonDictionary;
            }

            if (value is JArray array)
            {
                var result = new List(); // This is a python list.
                foreach (var item in array)
                {
                    result.Add(ForPython(item));
                }

                return result;
            }

            if (value is JObject @object)
            {
                var result = new PythonDictionary();
                foreach (var property in @object.Properties())
                {
                    result.Add(property.Name, ForPython(property.Value));
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
                    result.Add(ForPython(item));
                }

                return result;
            }
            
            return value;
        }

        private static object FromPython(PythonDictionary pythonDictionary)
        {
            if (pythonDictionary == null) throw new ArgumentNullException(nameof(pythonDictionary));

            var wirehomeDictionary = new WirehomeDictionary();
            foreach (var entry in pythonDictionary)
            {
                var key = Convert.ToString(entry.Key);
                wirehomeDictionary.TryAdd(key, FromPython(entry.Value));
            }

            return wirehomeDictionary;
        }
    }
}
