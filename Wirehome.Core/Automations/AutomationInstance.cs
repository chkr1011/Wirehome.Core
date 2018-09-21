using System;
using Wirehome.Core.Python;

namespace Wirehome.Core.Automations
{
    public class AutomationInstance
    {
        public AutomationInstance(PythonScriptHost scriptHost)
        {
            ScriptHost = scriptHost ?? throw new ArgumentNullException(nameof(scriptHost));
        }

        public PythonScriptHost ScriptHost { get; }
    }
}
