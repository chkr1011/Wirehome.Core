using IronPython.Runtime;
using System.Collections.Generic;
using Wirehome.Core.Python;

namespace Wirehome.Core.Foundation.Model
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
    }
}
