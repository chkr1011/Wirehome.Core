using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Contracts;

namespace Wirehome.Core.Diagnostics;

public sealed class SystemStatusService : WirehomeCoreService
{
    readonly ILogger<SystemStatusService> _logger;
    readonly List<Action<Dictionary<string, object>>> _valueProviders = new();
    readonly Dictionary<string, Func<object>> _values = new();

    public SystemStatusService(ILogger<SystemStatusService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Dictionary<string, object> All()
    {
        var result = new Dictionary<string, object>();

        lock (_values)
        {
            foreach (var value in _values)
            {
                object effectiveValue;

                try
                {
                    effectiveValue = value.Value();
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, $"Error while propagating value for system status '{value.Key}'.");
                    effectiveValue = null;
                }

                result[value.Key] = effectiveValue;
            }
        }

        lock (_valueProviders)
        {
            foreach (var valueProvider in _valueProviders)
            {
                try
                {
                    valueProvider(result);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Error while propagating values for system status.");
                }
            }
        }

        return result;
    }

    public void Delete(string uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        lock (_values)
        {
            _values.Remove(uid);
        }
    }

    public object Get(string uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        lock (_values)
        {
            if (!_values.TryGetValue(uid, out var valueProvider))
            {
                return null;
            }

            return valueProvider();
        }
    }

    public void RegisterProvider(Action<Dictionary<string, object>> provider)
    {
        if (provider is null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        lock (_valueProviders)
        {
            _valueProviders.Add(provider);
        }
    }

    public void Set(string uid, object value)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        lock (_values)
        {
            _values[uid] = () => value;
        }
    }

    public void Set(string uid, Func<object> valueProvider)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        lock (_values)
        {
            if (valueProvider == null)
            {
                _values[uid] = null;
            }
            else
            {
                _values[uid] = valueProvider;
            }
        }
    }
}