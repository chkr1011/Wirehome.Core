using System;
using System.Collections.Generic;
using System.Globalization;

namespace Wirehome.Core.MessageBus
{
    public static class MessageBusFilterComparer
    {
        public static bool IsMatch(IDictionary<object, object> message, IDictionary<object, object> filter)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            foreach (var filterEntry in filter)
            {
                if (!message.TryGetValue(filterEntry.Key, out var propertyValue))
                {
                    return false;
                }

                var pattern = ConvertValueToString(filterEntry.Value);
                var wildcard = "*".AsSpan();

                if (pattern.Equals(wildcard, StringComparison.Ordinal))
                {
                    continue;
                }

                var value = ConvertValueToString(propertyValue);

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

        private static ReadOnlySpan<char> ConvertValueToString(object value)
        {
            // This is required because some type checks are not supported
            // like `(int)5 == (long)5` which will result in `False`.
            // So a transformation over invariant strings is made.

            if (value == null)
            {
                return string.Empty.AsSpan();
            }

            if (value is string @string)
            {
                return @string.AsSpan();
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture).AsSpan();
        }
    }
}
