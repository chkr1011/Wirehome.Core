using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Constants;
using Wirehome.Core.GlobalVariables;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Resources
{
    /// <summary>
    /// TODO: Add support for blobs (byte[]). File pattern "Blob.en.bin" "Blob.de-DE.bin"
    /// </summary>
    public class ResourcesService
    {
        private readonly Dictionary<string, Resource> _resources = new Dictionary<string, Resource>();

        private readonly StorageService _storageService;
        private readonly GlobalVariablesService _globalVariablesService;
        private readonly ILogger _logger;

        public ResourcesService(StorageService storageService, GlobalVariablesService globalVariablesService, ILoggerFactory loggerFactory)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _globalVariablesService = globalVariablesService ?? throw new ArgumentNullException(nameof(globalVariablesService));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<ResourcesService>();
        }

        public void Start()
        {
            lock (_resources)
            {
                Load();
            }
        }

        public string FormatValue(string message, IDictionary parameters)
        {
            if (string.IsNullOrEmpty(message) || parameters == null || parameters.Count == 0)
            {
                return message;
            }
            
            foreach (var key in parameters.Keys)
            {
                var value = ConvertValue(parameters[key]);
                message = message.Replace("{" + key + "}", value);
            }

            return message;
        }

        public void RegisterString(string uid, string languageCode, string value)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (languageCode == null) throw new ArgumentNullException(nameof(languageCode));
            if (value == null) throw new ArgumentNullException(nameof(value));

            lock (_resources)
            {
                if (_resources.TryGetValue(uid, out var resource))
                {
                    if (resource.ContainsString(languageCode))
                    {
                        return;
                    }
                }

                SetString(uid, languageCode, value);
            }
        }

        public void SetString(string uid, string languageCode, string value)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (languageCode == null) throw new ArgumentNullException(nameof(languageCode));
            if (value == null) throw new ArgumentNullException(nameof(value));

            lock (_resources)
            {
                if (!_resources.TryGetValue(uid, out var resource))
                {
                    resource = new Resource();
                    _resources.Add(uid, resource);
                }

                var existingValue = resource.GetString(languageCode);
                if (string.Equals(existingValue, value))
                {
                    return;
                }

                resource.SetString(languageCode, value);
                Save();
            }
        }

        public string ResolveString(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var systemLanguageCode = _globalVariablesService.GetValue(GlobalVariableUids.LanguageCode) as string;
            return ResolveString(uid, systemLanguageCode);
        }

        public string ResolveString(string uid, string languageCode)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (languageCode == null) throw new ArgumentNullException(nameof(languageCode));

            lock (_resources)
            {
                if (_resources.TryGetValue(uid, out var resource))
                {
                    return resource.ResolveString(languageCode);
                }

                return null;
            }
        }

        private string ConvertValue(object value)
        {
            var systemLanguageCode = _globalVariablesService.GetValue(GlobalVariableUids.LanguageCode) as string ?? "en";
            var cultureInfo = CultureInfo.GetCultureInfo(systemLanguageCode);

            if (cultureInfo == null)
            {
                return Convert.ToString(value);
            }

            return Convert.ToString(value, cultureInfo);
        }

        private void Load()
        {
            _resources.Clear();

            var configurationDirectories = _storageService.EnumeratureDirectories("*", "Resources");
            foreach (var configurationDirectory in configurationDirectories)
            {
                var key = configurationDirectory;
                var resource = new Resource();

                if (_storageService.TryRead(out Dictionary<string, string> strings, "Resources", configurationDirectory, "Strings.json"))
                {
                    foreach (var @string in strings)
                    {
                        resource.SetString(@string.Key, @string.Value);
                    }
                }

                _resources[key] = resource;
            }
        }

        private void Save()
        {
            foreach (var resource in _resources)
            {
                _storageService.Write(resource.Value.GetStrings(), "Resources", resource.Key, "Strings.json");
            }
        }
    }
}
