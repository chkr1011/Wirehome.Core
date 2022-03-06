using IronPython.Runtime;

namespace Wirehome.Core.Python;

public static class PythonDelegates
{
    public delegate void CallbackDelegate(PythonDictionary parameters);

    public delegate PythonDictionary CallbackWithResultDelegate(PythonDictionary parameters);
}