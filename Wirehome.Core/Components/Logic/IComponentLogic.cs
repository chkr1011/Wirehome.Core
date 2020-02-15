using IronPython.Runtime;

namespace Wirehome.Core.Components.Logic
{
    public interface IComponentLogic
    {
        PythonDictionary ProcessMessage(PythonDictionary message);

        PythonDictionary GetDebugInformation(PythonDictionary parameters);
    }
}
