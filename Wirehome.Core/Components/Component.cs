using System;
using System.Collections.Generic;
using System.Threading;
using Wirehome.Core.Components.Logic;
using Wirehome.Core.Model;

namespace Wirehome.Core.Components
{
    public partial class Component
    {
        private readonly Dictionary<string, object> _status = new Dictionary<string, object>();
        private readonly Dictionary<string, object> _settings = new Dictionary<string, object>();
        private readonly Dictionary<string, object> _configuration = new Dictionary<string, object>();
        private readonly HashSet<string> _tags = new HashSet<string>();

        private IComponentLogic _logic;

        private long _hash;

        public Component(string uid)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
        }

        public string Uid { get; }

        public long Hash
        {
            get
            {
                return Interlocked.Read(ref _hash);
            }
        }

        public bool TryGetStatusValue(string key, out object value)
        {
            lock (_status)
            {
                return _status.TryGetValue(key, out value);
            }            
        }

        public SetValueResult SetStatusValue(string key, object value)
        {
            lock (_status)
            {
                var isExistingValue = _status.TryGetValue(key, out var oldValue);

                _status[key] = value;
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

        public bool TryGetSetting(string key, out object value)
        {
            lock (_settings)
            {
                return _settings.TryGetValue(key, out value);
            }
        }

        public SetValueResult SetSetting(string key, object value)
        {
            lock (_settings)
            {
                var isExistingValue = _settings.TryGetValue(key, out var oldValue);

                _settings[key] = value;
                IncrementHash();

                return new SetValueResult
                {
                    OldValue = oldValue,
                    IsNewValue = !isExistingValue
                };
            }
        }

        public bool RemoveSetting(string key, out object oldValue)
        {
            lock (_settings)
            {
                if (_settings.TryGetValue(key, out oldValue))
                {
                    _settings.Remove(key);
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

        public bool TryGetConfigurationValue(string key, out object value)
        {
            lock (_configuration)
            {
                return _configuration.TryGetValue(key, out value);
            }
        }

        public SetValueResult SetConfigurationValue(string key, object value)
        {
            lock (_configuration)
            {
                var isExistingValue = _configuration.TryGetValue(key, out var oldValue);

                _configuration[key] = value;
                IncrementHash();

                return new SetValueResult
                {
                    OldValue = oldValue,
                    IsNewValue = !isExistingValue
                };
            }
        }

        public bool RemoveConfigurationValue(string key, out object oldValue)
        {
            lock (_configuration)
            {
                if (_configuration.TryGetValue(key, out oldValue))
                {
                    _configuration.Remove(key);
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

        public bool SetTag(string tag)
        {
            if (tag is null) throw new ArgumentNullException(nameof(tag));
            
            lock (_tags)
            {
                if (_tags.Add(tag))
                {
                    IncrementHash();
                    return true;
                }

                return false;
            }
        }
               
        public bool RemoveTag(string tag)
        {
            if (tag is null) throw new ArgumentNullException(nameof(tag));

            lock (_tags)
            {
                if (_tags.Remove(tag))
                {
                    IncrementHash();
                    return true;
                }

                return false;
            }
        }

        public bool HasTag(string tag)
        {
            lock (_tags)
            {
                return _tags.Contains(tag);
            }
        }

        public WirehomeDictionary ProcessMessage(WirehomeDictionary message)
        {
            ThrowIfLogicNotSet();
            return _logic.ProcessMessage(message);
        }

        public WirehomeDictionary GetDebugInformation(WirehomeDictionary parameters)
        {
            ThrowIfLogicNotSet();
            return _logic.GetDebugInformation(parameters);
        }

        public void SetLogic(IComponentLogic logic)
        {
            if (_logic != null) throw new InvalidOperationException("A component logic cannot be changed.");

            _logic = logic ?? throw new ArgumentNullException(nameof(logic));
        }

        private void ThrowIfLogicNotSet()
        {
            if (_logic == null) throw new InvalidOperationException("A component requires a logic to process messages.");
        }

        private void IncrementHash()
        {
            Interlocked.Increment(ref _hash);
        }
    }
}
