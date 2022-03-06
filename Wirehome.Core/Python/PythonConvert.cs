using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using IronPython.Runtime;
using Newtonsoft.Json.Linq;

namespace Wirehome.Core.Python;

public static class PythonConvert
{
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

    public static JToken FromPythonToJson(object value)
    {
        if (value is PythonDictionary pythonDictionary)
        {
            var @object = new JObject();
            foreach (var item in pythonDictionary)
            {
                @object[Convert.ToString(item.Key, CultureInfo.InvariantCulture)] = FromPythonToJson(item.Value);
            }

            return @object;
        }

        if (value is List pythonList) // Python list
        {
            var array = new JArray();
            foreach (var item in pythonList)
            {
                array.Add(FromPythonToJson(item));
            }
        }

        return JToken.FromObject(value);
    }

    public static byte[] ToPayload(object payload)
    {
        if (payload == null)
        {
            return Array.Empty<byte>();
        }

        if (payload is ByteArray byteArray)
        {
            return byteArray.ToArray();
        }

        if (payload is byte[] byteArray2)
        {
            return byteArray2;
        }

        if (payload is Bytes bytes)
        {
            return bytes.GetUnsafeByteArray();
        }

        if (payload is string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }

        if (payload is List<byte> b)
        {
            return b.ToArray();
        }

        if (payload is IEnumerable<int> i)
        {
            return i.Select(Convert.ToByte).ToArray();
        }

        throw new NotSupportedException($"Payload format '{payload.GetType().Name}' is not supported.");
    }

    public static object ToPython(object value)
    {
        if (value is PythonDictionary)
        {
            return value;
        }

        if (value is List)
        {
            return value;
        }

        if (value is byte[] buffer)
        {
            return new Bytes(buffer);
        }

        if (value is null || value is string || value is bool)
        {
            return value;
        }

        if (value is int || value is float || value is long || value is double || value is Complex)
        {
            return value;
        }

        if (value is IDictionary<object, object> dictionary)
        {
            var pythonDictionary = new PythonDictionary();
            foreach (var entryKey in dictionary.Keys)
            {
                pythonDictionary.Add(entryKey, ToPython(dictionary[entryKey]));
            }

            return pythonDictionary;
        }

        if (value is MulticastDelegate)
        {
            return value;
        }

        if (value is Delegate)
        {
            return value;
        }

        // Convert JSON stuff.
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

            return ToPython(jValue.ToObject<object>());
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

    public static PythonDictionary ToPythonDictionary(IDictionary<object, object> source)
    {
        if (source == null)
        {
            return null;
        }

        if (source is PythonDictionary pythonDictionary)
        {
            return pythonDictionary;
        }

        pythonDictionary = new PythonDictionary();
        foreach (var item in source)
        {
            pythonDictionary.Add(item.Key, item.Value);
        }

        return pythonDictionary;
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

    public static IDictionary<object, object> ToWirehomeDictionary(PythonDictionary pythonDictionary)
    {
        if (pythonDictionary == null)
        {
            return null;
        }

        var result = new Dictionary<object, object>();
        foreach (var item in pythonDictionary)
        {
            result.Add(FromPython(item.Key), FromPython(item.Value));
        }

        return result;
    }
}