using IronPython.Runtime;

namespace Wirehome.Core.Python
{
    public static class PythonDelegates
    {
        public delegate PythonDictionary CallbackWithResultDelegate(PythonDictionary parameters);

        public delegate void CallbackDelegate(PythonDictionary parameters);
    }
}
