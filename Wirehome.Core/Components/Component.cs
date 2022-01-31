using System;
using System.Collections.Generic;
using System.Threading;
using Wirehome.Core.Components.Logic;
using Wirehome.Core.Python;

namespace Wirehome.Core.Components
{
    public sealed class Component
    {
        readonly Dictionary<string, object> _status = new();
        readonly Dictionary<string, object> _settings = new();
        readonly Dictionary<string, object> _configuration = new();
        readonly HashSet<string> _tags = new();

        IComponentLogic _logic;

        long _hash;

        public Component(string uid)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
        }

        public string Uid { get; }

        public long Hash => Interlocked.Read(ref _hash);

        public bool TryGetStatusValue(string uid, out object value)
        {
            lock (_status)
            {
                return _status.TryGetValue(uid, out value);
            }
        }

        public SetValueResult SetStatusValue(string uid, object value)
        {
            lock (_status)
            {
                var isExistingValue = _status.TryGetValue(uid, out var oldValue);

                _status[uid] = value;
                IncrementHash();

                return new SetValueResult
                {
                    OldValue = oldValue,
                    IsNewValue = !isExistingValue
                };
            }
        }

        public Dictionary<string, object> GetStatus()
        {
            lock (_status)
            {
                // Create a copy of the internal dictionary because the result only reflects the current
                // status and will not change when the real status is changing. Also changes to that dictionary
                // should not affect the internal state.
                return new Dictionary<string, object>(_status);
            }
        }

        public bool TryGetSetting(string uid, out object value)
        {
            lock (_settings)
            {
                return _settings.TryGetValue(uid, out value);
            }
        }

        public SetValueResult SetSetting(string uid, object value)
        {
            lock (_settings)
            {
                var isExistingValue = _settings.TryGetValue(uid, out var oldValue);

                _settings[uid] = value;
                IncrementHash();

                return new SetValueResult
                {
                    OldValue = oldValue,
                    IsNewValue = !isExistingValue
                };
            }
        }

        public bool RemoveSetting(string uid, out object oldValue)
        {
            lock (_settings)
            {
                if (_settings.TryGetValue(uid, out oldValue))
                {
                    _settings.Remove(uid);
                    IncrementHash();

                    return true;
                }

                return false;
            }
        }

        public Dictionary<string, object> GetSettings()
        {
            lock (_settings)
            {
                // Create a copy of the internal dictionary because the result only reflects the current
                // status and will not change when the real status is changing. Also changes to that dictionary
                // should not affect the internal state.
                return new Dictionary<string, object>(_settings);
            }
        }

        public bool TryGetConfigurationValue(string uid, out object value)
        {
            lock (_configuration)
            {
                return _configuration.TryGetValue(uid, out value);
            }
        }

        public SetValueResult SetConfigurationValue(string uid, object value)
        {
            lock (_configuration)
            {
                var isExistingValue = _configuration.TryGetValue(uid, out var oldValue);

                _configuration[uid] = value;
                IncrementHash();

                return new SetValueResult
                {
                    OldValue = oldValue,
                    IsNewValue = !isExistingValue
                };
            }
        }

        public bool RemoveConfigurationValue(string uid, out object oldValue)
        {
            lock (_configuration)
            {
                if (_configuration.TryGetValue(uid, out oldValue))
                {
                    _configuration.Remove(uid);
                    IncrementHash();

                    return true;
                }

                return false;
            }
        }

        public Dictionary<string, object> GetConfiguration()
        {
            lock (_configuration)
            {
                // Create a copy of the internal dictionary because the result only reflects the current
                // status and will not change when the real status is changing. Also changes to that dictionary
                // should not affect the internal state.
                return new Dictionary<string, object>(_configuration);
            }
        }

        public List<string> GetTags()
        {
            lock (_tags)
            {
                return new List<string>(_tags);
            }
        }

        public bool SetTag(string uid)
        {
            lock (_tags)
            {
                if (_tags.Add(uid))
                {
                    IncrementHash();
                    return true;
                }

                return false;
            }
        }

        public bool RemoveTag(string uid)
        {
            lock (_tags)
            {
                if (_tags.Remove(uid))
                {
                    IncrementHash();
                    return true;
                }

                return false;
            }
        }

        public bool HasTag(string uid)
        {
            lock (_tags)
            {
                return _tags.Contains(uid);
            }
        }

        public IDictionary<object, object> ProcessMessage(IDictionary<object, object> message)
        {
            ThrowIfLogicNotSet();
            return _logic.ProcessMessage(PythonConvert.ToPythonDictionary(message));
        }

        public IDictionary<object, object> GetDebugInformation(IDictionary<object, object> parameters)
        {
            ThrowIfLogicNotSet();
            return _logic.GetDebugInformation(PythonConvert.ToPythonDictionary(parameters));
        }

        public void SetLogic(IComponentLogic logic)
        {
            if (_logic != null) throw new InvalidOperationException("A component logic cannot be changed.");

            _logic = logic ?? throw new ArgumentNullException(nameof(logic));
        }

        public string GetLogicId()
        {
            return _logic?.Id;
        }

        void ThrowIfLogicNotSet()
        {
            if (_logic == null) throw new InvalidOperationException("A component requires a logic to process messages.");
        }

        void IncrementHash()
        {
            Interlocked.Increment(ref _hash);
        }
    }
}
