using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Constants;
using Wirehome.Core.Contracts;
using Wirehome.Core.GlobalVariables;
using Wirehome.Core.Resources.Exception;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Resources
{
    public class ResourceService : IService
    {
        private const string ResourcesDirectory = "Resources";
        private const string StringsFilename = "Strings.js";

        private readonly Dictionary<string, Dictionary<string, string>> _resources = new Dictionary<string, Dictionary<string, string>>();

        private readonly StorageService _storageService;
        private readonly JsonSerializerService _jsonSerializerService;
        private readonly GlobalVariablesService _globalVariablesService;
        private readonly ILogger _logger;

        public ResourceService(StorageService storageService, JsonSerializerService jsonSerializerService, GlobalVariablesService globalVariablesService, ILoggerFactory loggerFactory)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _jsonSerializerService = jsonSerializerService ?? throw new ArgumentNullException(nameof(jsonSerializerService));
            _globalVariablesService = globalVariablesService ?? throw new ArgumentNullException(nameof(globalVariablesService));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<ResourceService>();
        }

        public void Start()
        {
            foreach (var resourceUid in GetResourceUids())
            {
                _globalVariablesService.RegisterValue(GlobalVariableUids.LanguageCode, "en");

                TryLoadResource(resourceUid);
                TryLoadDefaultResources();
            }
        }

        public IList<string> GetResourceUids()
        {
            return _storageService.EnumeratureDirectories("*", ResourcesDirectory);
        }

        public IDictionary<string, string> GetResource(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_resources)
            {
                if (!_resources.TryGetValue(uid, out var resource))
                {
                    throw new ResourceNotFoundException(uid);
                }

                return resource;
            }
        }

        public void DeleteResource(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_resources)
            {
                if (_resources.Remove(uid))
                {
                    _storageService.DeleteDirectory(ResourcesDirectory, uid);
                }
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

        public void RegisterResource(string uid, string languageCode, string value)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (languageCode == null) throw new ArgumentNullException(nameof(languageCode));
            if (value == null) throw new ArgumentNullException(nameof(value));

            lock (_resources)
            {
                if (_resources.TryGetValue(uid, out var resource))
                {
                    if (resource.ContainsKey(languageCode))
                    {
                        return;
                    }
                }

                SetResourceValue(uid, languageCode, value);
            }
        }

        public void SetResourceValue(string uid, string languageCode, string value)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (languageCode == null) throw new ArgumentNullException(nameof(languageCode));
            if (value == null) throw new ArgumentNullException(nameof(value));

            lock (_resources)
            {
                if (!_resources.TryGetValue(uid, out var resource))
                {
                    resource = new Dictionary<string, string>();
                    _resources.Add(uid, resource);
                }

                if (resource.TryGetValue(languageCode, out var existingValue))
                {
                    if (string.Equals(existingValue, value, StringComparison.Ordinal))
                    {
                        return;
                    }
                }

                resource[languageCode] = value;

                Save();
            }
        }

        public string GetResourceValue(string uid, string languageCode, string defaultValue = "")
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (languageCode == null) throw new ArgumentNullException(nameof(languageCode));

            lock (_resources)
            {
                if (_resources.TryGetValue(uid, out var resource))
                {
                    if (resource.TryGetValue(languageCode, out var value))
                    {
                        return value;
                    }
                }

                return defaultValue;
            }
        }

        public string GetResourceValueInSystemLanguage(string uid, string defaultValue = "")
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var systemLanguageCode = _globalVariablesService.GetValue(GlobalVariableUids.LanguageCode) as string;
            return GetResourceValue(uid, systemLanguageCode, defaultValue);
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

        private void TryLoadResource(string uid)
        {
            if (!_storageService.TryRead(out Dictionary<string, string> strings, ResourcesDirectory, uid, StringsFilename))
            {
                strings = new Dictionary<string, string>();
            }

            lock (_resources)
            {
                _resources[uid] = strings ?? new Dictionary<string, string>();
            }
        }

        private void TryLoadDefaultResources()
        {
            var filename = Path.Combine(_storageService.BinPath, "WebApp", "resources.json");
            if (!_jsonSerializerService.TryDeserializeFile(filename, out Dictionary<string, Dictionary<string, string>> defaultResources))
            {
                return;
            }

            foreach (var defaultResource in defaultResources)
            {
                if (defaultResource.Value == null)
                {
                    continue;
                }

                foreach (var defaultResourceValue in defaultResource.Value)
                {
                    if (defaultResourceValue.Value == null)
                    {
                        continue;
                    }

                    RegisterResource(defaultResource.Key, defaultResourceValue.Key, defaultResourceValue.Value);
                }
            }
        }

        private void Save()
        {
            foreach (var resource in _resources)
            {
                _storageService.Write(resource.Value, ResourcesDirectory, resource.Key, StringsFilename);
            }

            _logger.LogInformation("Resources written to disk.");
        }
    }
}
