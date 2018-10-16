using System;
using System.Collections.Generic;
using IronPython.Runtime;
using Wirehome.Core.Python;

namespace Wirehome.Core.Model
{
    public class WirehomeDictionary : Dictionary<string, object>
    {
        public static implicit operator WirehomeDictionary(PythonDictionary pythonDictionary)
        {
            return PythonConvert.ToWirehomeDictionary(pythonDictionary);
        }

        public static implicit operator PythonDictionary(WirehomeDictionary wirehomeDictionary)
        {
            return PythonConvert.ToPythonDictionary(wirehomeDictionary);
        }

        public WirehomeDictionary WithType(string type)
        {
            return WithValue("type", type);
        }

        public WirehomeDictionary WithValue(string key, object value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            this[key] = value;
            return this;
        }
    }
}
