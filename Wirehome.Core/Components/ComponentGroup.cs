using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using static Wirehome.Core.Components.Component;

namespace Wirehome.Core.Components
{
    public class ComponentGroup
    {
        private readonly Dictionary<string, object> _status = new Dictionary<string, object>();
        private readonly Dictionary<string, object> _settings = new Dictionary<string, object>();
        private readonly HashSet<string> _tags = new HashSet<string>();

        private long _hash;

        public ComponentGroup(string uid)
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

        public List<string> GetTags()
        {
            lock (_tags)
            {
                return new List<string>(_tags);
            }
        }

        public bool SetTag(string tag)
        {
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

        public ConcurrentDictionary<string, ComponentGroupAssociation> Components { get; } = new ConcurrentDictionary<string, ComponentGroupAssociation>();

        public ConcurrentDictionary<string, ComponentGroupAssociation> Macros { get; } = new ConcurrentDictionary<string, ComponentGroupAssociation>();

        private void IncrementHash()
        {
            Interlocked.Increment(ref _hash);
        }
    }
}
