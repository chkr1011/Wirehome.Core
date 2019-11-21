using IronPython.Runtime;
using System.Collections.Concurrent;
using Wirehome.Core.Python;

namespace Wirehome.Core.Foundation.Model
{
    public class ConcurrentWirehomeDictionary : ConcurrentDictionary<string, object>
    {
        public static implicit operator ConcurrentWirehomeDictionary(PythonDictionary pythonDictionary)
        {
            return PythonConvert.ToConcurrentWirehomeDictionary(pythonDictionary);
        }

        public static implicit operator PythonDictionary(ConcurrentWirehomeDictionary wirehomeDictionary)
        {
            return PythonConvert.ToPythonDictionary(wirehomeDictionary);
        }
    }
}