using System;
using System.Globalization;
using Wirehome.Core.Model;

namespace Wirehome.Core.MessageBus
{
    public static class MessageBusFilterComparer
    {
        public static bool IsMatch(WirehomeDictionary message, WirehomeDictionary filter)
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

                if (pattern.Equals("*", StringComparison.Ordinal))
                {
                    continue;
                }

                var value = ConvertValueToString(propertyValue);

                if (pattern.EndsWith("*", StringComparison.Ordinal))
                {
                    if (!value.StartsWith(pattern.Substring(0, pattern.Length - 1), StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
                else if (pattern.StartsWith("*", StringComparison.Ordinal))
                {
                    if (!value.EndsWith(pattern.Substring(1), StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!string.Equals(value, pattern, StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
            }

            // A filter without any properties is matching always.
            return true;
        }

        private static string ConvertValueToString(object value)
        {
            // This is required because some type checks are not supported
            // like `(int)5 == (long)5` which will result in `False`.
            // So a transformation over invariant strings is made.

            if (value == null)
            {
                return string.Empty;
            }

            if (value is string @string)
            {
                return @string;
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }
    }
}
