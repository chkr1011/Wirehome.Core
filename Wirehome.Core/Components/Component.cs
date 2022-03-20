using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using IronPython.Runtime;
using Wirehome.Core.Components.Logic;
using Wirehome.Core.Python;

namespace Wirehome.Core.Components;

public sealed class Component
{
    readonly ConcurrentDictionary<string, object> _configuration = new();
    readonly ConcurrentDictionary<string, object> _settings = new();
    readonly ConcurrentDictionary<string, object> _status = new();
    readonly HashSet<string> _tags = new();

    long _hash;

    IComponentLogic _logic;

    public Component(string uid)
    {
        Uid = uid ?? throw new ArgumentNullException(nameof(uid));
    }

    public long Hash => Interlocked.Read(ref _hash);
    
    public string Uid { get; }

    public IReadOnlyDictionary<string, object> GetConfiguration()
    {
        return new ReadOnlyDictionary<string, object>(_configuration);
    }

    public PythonDictionary GetDebugInformation(PythonDictionary parameters)
    {
        ThrowIfLogicNotSet();
        return _logic.GetDebugInformation(PythonConvert.ToPythonDictionary(parameters));
    }

    public string GetLogicId()
    {
        return _logic?.Id;
    }

    public IReadOnlyDictionary<string, object> GetSettings()
    {
        return new ReadOnlyDictionary<string, object>(_settings);
    }

    public IReadOnlyDictionary<string, object> GetStatus()
    {
        return new ReadOnlyDictionary<string, object>(_status);
    }

    public IReadOnlyList<string> GetTags()
    {
        lock (_tags)
        {
            return new List<string>(_tags);
        }
    }

    public bool HasTag(string uid)
    {
        lock (_tags)
        {
            return _tags.Contains(uid);
        }
    }

    public PythonDictionary ProcessMessage(PythonDictionary message)
    {
        ThrowIfLogicNotSet();
        return _logic.ProcessMessage(message);
    }

    public bool RemoveConfigurationValue(string uid, out object oldValue)
    {
        lock (_configuration)
        {
            if (_configuration.TryRemove(uid, out oldValue))
            {
                IncrementHash();

                return true;
            }

            return false;
        }
    }

    public bool RemoveSetting(string uid, out object oldValue)
    {
        lock (_settings)
        {
            if (_settings.TryRemove(uid, out oldValue))
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

    public void SetLogic(IComponentLogic logic)
    {
        if (_logic != null)
        {
            throw new InvalidOperationException("A component logic cannot be changed.");
        }

        _logic = logic ?? throw new ArgumentNullException(nameof(logic));
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

    public bool TryGetConfigurationValue(string uid, out object value)
    {
        lock (_configuration)
        {
            return _configuration.TryGetValue(uid, out value);
        }
    }

    public bool TryGetSetting(string uid, out object value)
    {
        lock (_settings)
        {
            return _settings.TryGetValue(uid, out value);
        }
    }

    public bool TryGetStatusValue(string uid, out object value)
    {
        lock (_status)
        {
            return _status.TryGetValue(uid, out value);
        }
    }

    void IncrementHash()
    {
        Interlocked.Increment(ref _hash);
    }

    void ThrowIfLogicNotSet()
    {
        if (_logic == null)
        {
            throw new InvalidOperationException("A component requires a logic to process messages.");
        }
    }
}