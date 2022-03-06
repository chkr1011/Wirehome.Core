using System;
using System.Collections.Generic;

namespace Wirehome.Core.MessageBus;

public static class MessageBusFilterComparer
{
    public static bool IsMatch(IDictionary<string, string> message, IDictionary<string, string> filter)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (filter == null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        var wildcard = "*".AsSpan();

        foreach (var filterEntry in filter)
        {
            if (!message.TryGetValue(filterEntry.Key, out var propertyValue))
            {
                return false;
            }

            var pattern = filterEntry.Value.AsSpan();

            if (pattern.Equals(wildcard, StringComparison.Ordinal))
            {
                continue;
            }

            var value = propertyValue.AsSpan();

            if (pattern.EndsWith(wildcard, StringComparison.Ordinal))
            {
                if (!value.StartsWith(pattern.Slice(0, pattern.Length - 1), StringComparison.Ordinal))
                {
                    return false;
                }
            }
            else if (pattern.StartsWith(wildcard, StringComparison.Ordinal))
            {
                if (!value.EndsWith(pattern.Slice(1), StringComparison.Ordinal))
                {
                    return false;
                }
            }
            else
            {
                if (!value.Equals(pattern, StringComparison.Ordinal))
                {
                    return false;
                }
            }
        }

        // A filter without any properties is matching always.
        return true;
    }
}