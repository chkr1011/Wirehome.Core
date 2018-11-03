using System;
using Wirehome.Core.Python;
using Wirehome.Core.ServiceHost.Configuration;

namespace Wirehome.Core.ServiceHost
{
    public class ServiceInstance
    {
        private readonly object _syncRoot = new object();
        private readonly PythonScriptHost _scriptHost;

        public ServiceInstance(string id, ServiceConfiguration configuration, PythonScriptHost scriptHost)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _scriptHost = scriptHost ?? throw new ArgumentNullException(nameof(scriptHost));
        }

        public string Id { get; }

        public ServiceConfiguration Configuration { get; }

        public void SetVariable(string key, object value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            lock (_syncRoot)
            {
                _scriptHost.SetVariable(key, value);
            }
        }

        public object ExecuteFunction(string name, params object[] parameters)
        {
            lock (_syncRoot)
            {
                return _scriptHost.InvokeFunction(name, parameters);
            }
        }
    }
}
