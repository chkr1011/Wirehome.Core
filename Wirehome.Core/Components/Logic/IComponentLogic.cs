using IronPython.Runtime;

namespace Wirehome.Core.Components.Logic
{
    public interface IComponentLogic
    {
        string Id { get; }

        PythonDictionary ProcessMessage(PythonDictionary message);

        PythonDictionary GetDebugInformation(PythonDictionary parameters);
    }
}
