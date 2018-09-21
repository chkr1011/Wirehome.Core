using System;
using System.Globalization;
using Wirehome.Core.Model;

namespace Wirehome.Core.MessageBus
{
    public class MessageBusSubscriber
    {
        private readonly WirehomeDictionary _filter;
        private readonly Action<WirehomeDictionary> _callback;

        public MessageBusSubscriber(string uid, WirehomeDictionary filter, Action<WirehomeDictionary> callback)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
            _filter = filter ?? throw new ArgumentNullException(nameof(filter));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public string Uid { get; }

        public bool IsFilterMatch(WirehomeDictionary message)
        {
            foreach (var filterEntry in _filter)
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

        public void Notify(WirehomeDictionary message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            _callback(message);
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