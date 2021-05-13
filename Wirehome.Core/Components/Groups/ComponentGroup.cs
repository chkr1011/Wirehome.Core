using System;
using System.Collections.Generic;
using System.Threading;
using Wirehome.Core.Foundation;

namespace Wirehome.Core.Components.Groups
{
    public sealed class ComponentGroup
    {
        readonly Dictionary<string, object> _status = new Dictionary<string, object>();
        readonly Dictionary<string, object> _settings = new Dictionary<string, object>();
        readonly HashSet<string> _tags = new HashSet<string>();

        long _hash;

        public ComponentGroup(string uid)
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

        public ThreadSafeDictionary<string, ComponentGroupAssociation> Components { get; } = new ThreadSafeDictionary<string, ComponentGroupAssociation>();

        public ThreadSafeDictionary<string, ComponentGroupAssociation> Macros { get; } = new ThreadSafeDictionary<string, ComponentGroupAssociation>();

        void IncrementHash()
        {
            Interlocked.Increment(ref _hash);
        }
    }
}
