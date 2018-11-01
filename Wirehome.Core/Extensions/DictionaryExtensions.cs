using System;
using System.Collections;
using System.Globalization;

namespace Wirehome.Core.Extensions
{
    public static class DictionaryExtensions
    {
        public static TValue GetValueOr<TValue>(this IDictionary dictionary, object key, TValue defaultValue)
        {
            if (!dictionary.Contains(key))
            {
                return defaultValue;
            }

            var value = dictionary[key];

            return (TValue)Convert.ChangeType(value, typeof(TValue), CultureInfo.InvariantCulture);
        }

        public static void SetValue(this IDictionary dictionary, string key, object value)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
            if (key == null) throw new ArgumentNullException(nameof(key));

            dictionary[key] = value;
        }

        public static TDictionary WithType<TDictionary>(this TDictionary dictionary, string type) where TDictionary : IDictionary
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));

            return WithValue(dictionary, "type", type);
        }

        public static TDictionary WithValue<TDictionary>(this TDictionary dictionary, string key, object value) where TDictionary : IDictionary
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
            if (key == null) throw new ArgumentNullException(nameof(key));

            dictionary[key] = value;
            return dictionary;
        }
    }
}
