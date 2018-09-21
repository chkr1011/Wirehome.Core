using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Wirehome.Core.Resources
{
    public class Resource
    {
        private readonly Dictionary<string, string> _strings = new Dictionary<string, string>();

        public bool ContainsString(string languageCode)
        {
            lock (_strings)
            {
                return _strings.ContainsKey(languageCode);
            }
        }

        public string GetString(string languageCode)
        {
            if (languageCode == null) throw new ArgumentNullException(nameof(languageCode));

            lock (_strings)
            {
                if (_strings.TryGetValue(languageCode, out var value))
                {
                    return value;
                }

                return null;
            }
        }

        public IDictionary<string, string> GetStrings()
        {
            lock (_strings)
            {
                return _strings.ToDictionary(i => i.Key, i => i.Value);
            }
        }

        public void SetString(string languageCode, string value)
        {
            if (languageCode == null) throw new ArgumentNullException(nameof(languageCode));

            lock (_strings)
            {
                _strings[languageCode] = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        public string ResolveString(string languageCode)
        {
            if (languageCode == null) throw new ArgumentNullException(nameof(languageCode));

            lock (_strings)
            {
                if (_strings.TryGetValue(languageCode, out var value))
                {
                    return value;
                }

                if (_strings.TryGetValue(CultureInfo.InvariantCulture.TwoLetterISOLanguageName, out value))
                {
                    return value;
                }

                if (_strings.TryGetValue("en", out value))
                {
                    return value;
                }

                return null;
            }
        }
    }
}
