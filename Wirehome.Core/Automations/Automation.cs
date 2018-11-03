using System;
using Wirehome.Core.Model;
using Wirehome.Core.Python;

namespace Wirehome.Core.Automations
{
    public class Automation
    {
        private readonly PythonScriptHost _scriptHost;
        
        public Automation(string uid, PythonScriptHost scriptHost)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
            _scriptHost = scriptHost ?? throw new ArgumentNullException(nameof(scriptHost));
        }

        public string Uid { get; }

        public ConcurrentWirehomeDictionary Settings { get; } = new ConcurrentWirehomeDictionary();

        public WirehomeDictionary GetStatus()
        {
            if (!_scriptHost.FunctionExists("get_status"))
            {
                return new WirehomeDictionary();
            }

            return (WirehomeDictionary)_scriptHost.InvokeFunction("get_status");
        }

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
