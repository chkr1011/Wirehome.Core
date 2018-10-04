using System;
using System.Collections.Concurrent;
using IronPython.Runtime;

namespace Wirehome.Core.Model
{
    public class WirehomeDictionary : ConcurrentDictionary<string, object>
    {
        public static implicit operator WirehomeDictionary(PythonDictionary dictionary)
        {
            if (dictionary == null)
            {
                return null;
            }

            return WirehomeDictionaryConvert.FromObject(dictionary);
        }

        public WirehomeDictionary WithType(string type)
        {
            this["type"] = type ?? throw new ArgumentNullException(nameof(type));
            return this;
        }

        public WirehomeDictionary WithValue(string key, object value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            this[key] = value;
            return this;
        }
    }
}
