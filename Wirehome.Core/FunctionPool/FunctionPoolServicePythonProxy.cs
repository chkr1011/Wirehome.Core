#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using IronPython.Runtime;
using Wirehome.Core.Python;

namespace Wirehome.Core.FunctionPool;

public class FunctionPoolServicePythonProxy : IInjectedPythonProxy
{
    readonly FunctionPoolService _functionPoolService;

    public FunctionPoolServicePythonProxy(FunctionPoolService functionPoolService)
    {
        _functionPoolService = functionPoolService ?? throw new ArgumentNullException(nameof(functionPoolService));
    }

    public string ModuleName { get; } = "function_pool";

    public bool function_exists(string uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        return _functionPoolService.GetRegisteredFunctions().Contains(uid);
    }

    public PythonDictionary invoke(string uid, PythonDictionary parameters)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        return PythonConvert.ToPythonDictionary(_functionPoolService.InvokeFunction(uid, parameters));
    }

    public void register(string uid, PythonDelegates.CallbackWithResultDelegate function)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        if (function == null)
        {
            throw new ArgumentNullException(nameof(function));
        }

        _functionPoolService.RegisterFunction(uid, p => function(PythonConvert.ToPythonDictionary(p)));
    }
}