using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text;
using IronPython.Runtime;
using Newtonsoft.Json.Linq;
using Wirehome.Core.Model;

namespace Wirehome.Core.Python
{
    public static class PythonConvert
    {
        public static JToken FromPythonToJson(object value)
        {
            if (value is PythonDictionary d)
            {
                var @object = new JObject();
                foreach (var item in d)
                {
                    @object[Convert.ToString(item.Key, CultureInfo.InvariantCulture)] = FromPythonToJson(item.Value);
                }

                return @object;
            }

            if (value is List l) // Python list
            {
                var array = new JArray();
                foreach (var item in l)
                {
                    array.Add(FromPythonToJson(item));
                }
            }

            return JToken.FromObject(value);
        }

        public static object FromPython(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is PythonDictionary pythonDictionary)
            {
                return ToWirehomeDictionary(pythonDictionary);
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

        public static object ToPython(object value)
        {
            if (value is null || value is string || value is bool)
            {
                return value;
            }

            if (value is int || value is float || value is long || value is double || value is Complex)
            {
                return value;
            }

            if (value is IPythonConvertible pythonConvertible)
            {
                return pythonConvertible.ConvertToPython();
            }

            if (value is WirehomeDictionary wirehomeDictionary)
            {
                return ToPythonDictionary(wirehomeDictionary);
            }

            if (value is IDictionary dictionary)
            {
                var pythonDictionary = new PythonDictionary();
                foreach (var entryKey in dictionary.Keys)
                {
                    pythonDictionary.Add(entryKey, ToPython(dictionary[entryKey]));
                }

                return pythonDictionary;
            }

            if (value is JArray array)
            {
                var result = new List(); // This is a python list.
                foreach (var item in array)
                {
                    result.Add(ToPython(item));
                }

                return result;
            }

            if (value is JObject @object)
            {
                var result = new PythonDictionary();
                foreach (var property in @object.Properties())
                {
                    result.Add(property.Name, ToPython(property.Value));
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
                    result.Add(ToPython(item));
                }

                return result;
            }

            return value;
        }

        public static PythonDictionary ToPythonDictionary(IDictionary dictionary)
        {
            if (dictionary == null)
            {
                return null;
            }

            if (dictionary is PythonDictionary pythonDictionary)
            {
                return pythonDictionary;
            }

            var newPythonDictionary = new PythonDictionary();
            foreach (var entry in dictionary.Keys)
            {
                var key = Convert.ToString(entry, CultureInfo.InvariantCulture);
                newPythonDictionary.Add(key, ToPython(dictionary[entry]));
            }

            return newPythonDictionary;
        }

        public static WirehomeDictionary ToWirehomeDictionary(PythonDictionary pythonDictionary)
        {
            if (pythonDictionary == null)
            {
                return null;
            }

            var wirehomeDictionary = new WirehomeDictionary();
            foreach (var entry in pythonDictionary)
            {
                var key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture);
                wirehomeDictionary.TryAdd(key, FromPython(entry.Value));
            }

            return wirehomeDictionary;
        }

        public static ConcurrentWirehomeDictionary ToConcurrentWirehomeDictionary(PythonDictionary pythonDictionary)
        {
            if (pythonDictionary == null)
            {
                return null;
            }

            var wirehomeDictionary = new ConcurrentWirehomeDictionary();
            foreach (var entry in pythonDictionary)
            {
                var key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture);
                wirehomeDictionary.TryAdd(key, FromPython(entry.Value));
            }

            return wirehomeDictionary;
        }

        public static List ToPythonList(IEnumerable items)
        {
            if (items == null)
            {
                return null;
            }

            var list = new List();
            foreach (var item in items)
            {
                list.Add(ToPython(item));
            }

            return list;
        }

        public static PythonDictionary ToPythonDictionary(object source)
        {
            if (source == null)
            {
                return null;
            }

            if (source is WirehomeDictionary wirehomeDictionary)
            {
                return ToPythonDictionary(wirehomeDictionary);
            }

            var result = new PythonDictionary();

            var properties = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                var key = PythonfyPropertyName(property.Name);
                var value = ToPython(property.GetValue(source));
                result[key] = value;
            }

            return result;
        }

        public static string PythonfyPropertyName(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            var output = new StringBuilder();
            for (var i = 0; i < name.Length; i++)
            {
                var @char = name[i];

                if (i > 0 && (char.IsUpper(@char) || char.IsNumber(@char)))
                {
                    output.Append('_');
                }

                output.Append(char.ToLowerInvariant(@char));
            }

            return output.ToString();
        }
    }
}
