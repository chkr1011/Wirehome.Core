using System;
using System.Collections;
using System.Text;

namespace Wirehome.Core.Extensions
{
    public static class DictionaryExtensions
    {
        public static object GetValueOrDefault(this IDictionary dictionary, object key, object defaultValue = null)
        {
            if (!dictionary.Contains(key))
            {
                return defaultValue;
            }

            return dictionary[key];
        }
        
        public static void SetValue(this IDictionary dictionary, string key, object value)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
            if (key == null) throw new ArgumentNullException(nameof(key));

            dictionary[key] = value;
        }

        public static string ToExtendedString(this IDictionary dictionary)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));

            var result = new StringBuilder();
            result.Append("{ ");

            // TODO: Sort keys.

            var isFirstEntry = true;
            foreach (var key in dictionary.Keys)
            {
                var value = dictionary[key];
                if (value is string)
                {
                    value = "\"" + value + "\"";
                }

                if (!isFirstEntry)
                {
                    result.Append(", ");
                }

                result.Append("\"" + key + "\": " + Convert.ToString(value));

                isFirstEntry = false;
            }

            result.Append(" }");

            return result.ToString();
        }

        public static IDictionary WithType(this IDictionary d, string type)
        {
            return WithValue(d, "type", type);
        }

        public static IDictionary WithValue(this IDictionary d, string key, object value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            d[key] = value;
            return d;
        }
    }
}
