using System;

namespace Wirehome.Core.Foundation.Model
{
    public static class WirehomeDictionaryExtensions
    {
        public static WirehomeDictionary Clone(this WirehomeDictionary wirehomeDictionary)
        {
            if (wirehomeDictionary == null)
            {
                return null;
            }

            var clone = new WirehomeDictionary();
            foreach (var item in wirehomeDictionary)
            {
                clone.Add(item.Key, item.Value);
            }

            return clone;
        }

        public static WirehomeDictionary WithType(this WirehomeDictionary wirehomeDictionary, string type)
        {
            if (wirehomeDictionary == null) throw new ArgumentNullException(nameof(wirehomeDictionary));

            return wirehomeDictionary.WithValue("type", type);
        }

        public static WirehomeDictionary WithValue(this WirehomeDictionary wirehomeDictionary, string key, object value)
        {
            if (wirehomeDictionary == null) throw new ArgumentNullException(nameof(wirehomeDictionary));
            if (key == null) throw new ArgumentNullException(nameof(key));

            wirehomeDictionary[key] = value;
            return wirehomeDictionary;
        }
    }
}
