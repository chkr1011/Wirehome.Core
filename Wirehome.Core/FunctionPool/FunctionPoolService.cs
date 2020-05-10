using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Wirehome.Core.Constants;
using Wirehome.Core.Contracts;

namespace Wirehome.Core.FunctionPool
{
    public class FunctionPoolService : IService
    {
        private readonly Dictionary<string, Func<IDictionary<object, object>, IDictionary<object, object>>> _functions = new Dictionary<string, Func<IDictionary<object, object>, IDictionary<object, object>>>();

        private readonly ILogger<FunctionPoolService> _logger;

        public FunctionPoolService(ILogger<FunctionPoolService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Start()
        {
        }

        public List<string> GetRegisteredFunctions()
        {
            lock (_functions)
            {
                return _functions.Keys.ToList();
            }
        }

        public void RegisterFunction(string uid, Func<IDictionary<object, object>, IDictionary<object, object>> function)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (function == null) throw new ArgumentNullException(nameof(function));

            lock (_functions)
            {
                _functions[uid] = function;
            }

            _logger.LogDebug($"Function '{uid}' registered.");
        }

        public bool FunctionRegistered(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_functions)
            {
                return _functions.ContainsKey(uid);
            }
        }

        public IDictionary<object, object> InvokeFunction(string uid, IDictionary<object, object> parameters)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            Func<IDictionary<object, object>, IDictionary<object, object>> function;
            lock (_functions)
            {
                if (!_functions.TryGetValue(uid, out function))
                {
                    throw new NotSupportedException();
                }
            }

            try
            {
                return function(parameters ?? new PythonDictionary());
            }
            catch (Exception exception)
            {
                return new Dictionary<object, object>
                {
                    ["type"] = WirehomeMessageType.Exception,
                    ["exception.type"] = exception.GetType().Name,
                    ["exception.message"] = exception.Message,
                    ["exception.stack_trace"] = exception.StackTrace,
                };
            }
        }
    }
}