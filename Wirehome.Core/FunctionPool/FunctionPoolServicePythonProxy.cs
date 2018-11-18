#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using Wirehome.Core.Model;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Proxies;

namespace Wirehome.Core.FunctionPool
{
    public class FunctionPoolServicePythonProxy : IInjectedPythonProxy
    {
        private readonly FunctionPoolService _functionPoolService;

        public FunctionPoolServicePythonProxy(FunctionPoolService functionPoolService)
        {
            _functionPoolService = functionPoolService ?? throw new ArgumentNullException(nameof(functionPoolService));
        }

        public string ModuleName { get; } = "function_pool";

        public bool function_exists(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            return _functionPoolService.GetRegisteredFunctions().Contains(uid);
        }

        public WirehomeDictionary invoke(string uid, WirehomeDictionary parameters)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            return _functionPoolService.InvokeFunction(uid, parameters);
        }

        public void register(string uid, Func<WirehomeDictionary, WirehomeDictionary> function)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (function == null) throw new ArgumentNullException(nameof(function));

            _functionPoolService.RegisterFunction(uid, function);
        }
    }
}
