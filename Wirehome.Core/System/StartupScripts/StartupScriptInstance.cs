using System;
using Wirehome.Core.Python;

namespace Wirehome.Core.System.StartupScripts
{
    public class StartupScriptInstance
    {
        public StartupScriptInstance(string uid, PythonScriptHost scriptHost)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
            ScriptHost = scriptHost ?? throw new ArgumentNullException(nameof(scriptHost));
        }

        public string Uid { get; }

        public PythonScriptHost ScriptHost { get; }
    }
}
