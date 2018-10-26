using System;
using Wirehome.Core.Model;
using Wirehome.Core.Python;

namespace Wirehome.Core.Automations
{
    public class Automation
    {
        private readonly PythonScriptHost _scriptHost;

        public Automation(PythonScriptHost scriptHost)
        {
            _scriptHost = scriptHost ?? throw new ArgumentNullException(nameof(scriptHost));
        }

        public ConcurrentWirehomeDictionary Settings { get; } = new ConcurrentWirehomeDictionary();

        public void Initialize()
        {
            _scriptHost.InvokeFunction("initialize");
        }

        public void Activate()
        {
            _scriptHost.InvokeFunction("activate");
        }

        public void Deactivate()
        {
            _scriptHost.InvokeFunction("deactivate");
        }
    }
}
