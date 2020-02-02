using IronPython.Runtime;
using Wirehome.Core.Python;

namespace Wirehome.Core.Foundation.Model
{
    public class ConcurrentWirehomeDictionary : ThreadSafeDictionary<string, object>
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