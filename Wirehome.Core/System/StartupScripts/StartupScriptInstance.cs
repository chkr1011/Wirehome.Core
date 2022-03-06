using System;
using Wirehome.Core.Python;

namespace Wirehome.Core.System.StartupScripts;

public sealed class StartupScriptInstance
{
    readonly PythonScriptHost _scriptHost;
    readonly object _syncRoot = new();

    public StartupScriptInstance(string uid, StartupScriptConfiguration configuration, PythonScriptHost scriptHost)
    {
        Uid = uid ?? throw new ArgumentNullException(nameof(uid));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _scriptHost = scriptHost ?? throw new ArgumentNullException(nameof(scriptHost));
    }

    public StartupScriptConfiguration Configuration { get; }

    public string Uid { get; }

    public bool FunctionExists(string name)
    {
        lock (_scriptHost)
        {
            return _scriptHost.FunctionExists(name);
        }
    }

    public object InvokeFunction(string name, params object[] parameters)
    {
        lock (_syncRoot)
        {
            return _scriptHost.InvokeFunction(name, parameters);
        }
    }
}