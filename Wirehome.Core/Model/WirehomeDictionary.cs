using System;
using System.Collections.Concurrent;
using IronPython.Runtime;
using Wirehome.Core.Python;

namespace Wirehome.Core.Model
{
    // TODO: Consider create a "ConcurrentWirehomeDictionary" and use it for "Status", "Settings" etc. but not for parameters.
    public class WirehomeDictionary : ConcurrentDictionary<string, object>
    {
        public static implicit operator WirehomeDictionary(PythonDictionary pythonDictionary)
        {
            if (pythonDictionary == null)
            {
                return null;
            }

            return PythonConvert.ToWirehomeDictionary(pythonDictionary);
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
