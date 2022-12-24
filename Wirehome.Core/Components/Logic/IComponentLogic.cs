using IronPython.Runtime;

namespace Wirehome.Core.Components.Logic;

public interface IComponentLogic
{
    string Id { get; }

    PythonDictionary GetDebugInformation(PythonDictionary parameters);

    PythonDictionary ProcessMessage(PythonDictionary message);
}