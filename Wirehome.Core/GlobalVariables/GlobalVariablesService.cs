using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Model;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Proxies;
using Wirehome.Core.Storage;

namespace Wirehome.Core.GlobalVariables
{
    public class GlobalVariablesService
    {
        private readonly Dictionary<string, object> _variables = new Dictionary<string, object>();

        private readonly StorageService _storageService;
        private readonly MessageBusService _messageBusService;
        private readonly ILogger _logger;

        public GlobalVariablesService(StorageService storageService, PythonEngineService pythonEngineService, MessageBusService messageBusService, ILoggerFactory loggerFactory)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<GlobalVariablesService>();

            if (pythonEngineService == null) throw new ArgumentNullException(nameof(pythonEngineService));
            pythonEngineService.RegisterSingletonProxy(new GlobalVariablesPythonProxy(this));
        }

        public void Start()
        {
            lock (_variables)
            {
                Load();
            }
        }

        public void RegisterValue(string uid, object value)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_variables)
            {
                if (!_variables.ContainsKey(uid))
                {
                    _variables.Add(uid, value);
                }
            }
        }

        public IDictionary<string, object> GetValues()
        {
            var result = new Dictionary<string, object>();
            lock (_variables)
            {
                foreach (var variable in _variables)
                {
                    result[variable.Key] = variable.Value;
                }
            }

            return result;
        }

        public object GetValue(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_variables)
            {
                if (!_variables.TryGetValue(uid, out var value))
                {
                    _logger.Log(LogLevel.Warning, $"Requested global variable '{uid}' not set.");
                    return null;
                }

                return value;
            }
        }

        public bool TryGetValue<TValue>(string uid, TValue defaultValue, out TValue value)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            value = defaultValue;

            object existingValue;
            lock (_variables)
            {
                if (!_variables.TryGetValue(uid, out existingValue))
                {
                    _logger.Log(LogLevel.Warning, $"Requested global variable '{uid}' not set.");
                    return false;
                }
            }

            if (existingValue is TValue valueBuffer)
            {
                value = valueBuffer;
                return true;
            }

            return false;
        }

        public bool ValueExists(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_variables)
            {
                return _variables.ContainsKey(uid);
            }
        }

        public void SetValue(string uid, object value)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var busMessage = new WirehomeDictionary()
                .WithType("global_variables.event.value_set")
                .WithValue("uid", uid)
                .WithValue("new_value", value);
            
            lock (_variables)
            {
                if (_variables.TryGetValue(uid, out var oldValue))
                {
                    if (Equals(oldValue, value))
                    {
                        return;
                    }

                    busMessage.WithValue("old_value", oldValue);
                }
                
                _variables[uid] = value;

                Save();
            }

            _messageBusService.Publish(busMessage);
        }

        public void DeleteValue(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_variables)
            {
                _variables.Remove(uid);
            }

            var busMessage = new WirehomeDictionary()
                .WithType("global_variables.event.value_deleted")
                .WithValue("uid", uid);

            _messageBusService.Publish(busMessage);
        }

        private void Load()
        {
            if (_storageService.TryRead(out Dictionary<string, object> globalVariables, "GlobalVariables.json"))
            {
                foreach (var variable in globalVariables)
                {
                    _variables[variable.Key] = variable.Value;
                }
            }
        }

        private void Save()
        {
            _storageService.Write(_variables, "GlobalVariables.json");
        }
    }
}
