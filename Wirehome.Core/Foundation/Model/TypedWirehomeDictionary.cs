using IronPython.Runtime;
using Wirehome.Core.Python;

namespace Wirehome.Core.Foundation.Model
{
    public abstract class TypedWirehomeDictionary : IPythonConvertible
    {
        public string Type { get; set; } = "success";

        public object ConvertToPython()
        {
            return PythonConvert.ToPythonDictionary(this);
        }

        public PythonDictionary ConvertToPythonDictionary()
        {
            return PythonConvert.ToPythonDictionary(this);
        }

        public WirehomeDictionary ConvertToWirehomeDictionary()
        {
            return PythonConvert.ToPythonDictionary(this);
        }
    }
}
