using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Wirehome.Core.Constants;
using Wirehome.Core.Contracts;
using Wirehome.Core.GlobalVariables;
using Wirehome.Core.Resources.Exceptions;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Resources
{
    public sealed class ResourceService : WirehomeCoreService
    {
        const string ResourcesDirectory = "Resources";
        const string StringsFilename = "Strings.json";

        readonly Dictionary<string, Dictionary<string, string>> _resources = new();

        readonly StorageService _storageService;
        readonly JsonSerializerService _jsonSerializerService;
        readonly GlobalVariablesService _globalVariablesService;
        readonly ILogger _logger;

        public ResourceService(
            StorageService storageService,
            JsonSerializerService jsonSerializerService,
            GlobalVariablesService globalVariablesService,
            ILogger<ResourceService> logger)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _jsonSerializerService = jsonSerializerService ?? throw new ArgumentNullException(nameof(jsonSerializerService));
            _globalVariablesService = globalVariablesService ?? throw new ArgumentNullException(nameof(globalVariablesService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Dictionary<string, string> GetResources(string languageCode)
        {
            lock (_resources)
            {
                var resources = new Dictionary<string, string>();
                foreach (var resource in _resources)
                {
                    resources.Add(resource.Key, GetLanguageResourceValue(resource.Key, languageCode));
                }

                return resources;
            }
        }

        public IList<string> GetResourceUids()
        {
            return _storageService.EnumerateDirectories("*", ResourcesDirectory);
        }

        public IDictionary<string, string> GetResourceDefinition(string uid)
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
                    _storageService.DeletePath(ResourcesDirectory, uid);
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
                message = message.Replace("{" + key + "}", value, StringComparison.Ordinal);
            }

            return message;
        }

        public bool RegisterResource(string uid, string languageCode, string value)
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
                        return false;
                    }
                }

                SetResourceValue(uid, languageCode, value);
                return true;
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

        public string GetLanguageResourceValue(string uid, string languageCode, string defaultValue = "")
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

                    if (resource.TryGetValue("", out value))
                    {
                        return value;
                    }
                }

                return defaultValue;
            }
        }

        public string GetResourceValue(string uid, string defaultValue = "")
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var systemLanguageCode = _globalVariablesService.GetValue(GlobalVariableUids.LanguageCode) as string;
            return GetLanguageResourceValue(uid, systemLanguageCode, defaultValue);
        }

        protected override void OnStart()
        {
            foreach (var resourceUid in GetResourceUids())
            {
                TryLoadResource(resourceUid);
            }

            var filename = Path.Combine(_storageService.BinPath, "Resources.json");
            TryRegisterDefaultResources(filename);
        }

        string ConvertValue(object value)
        {
            var systemLanguageCode = _globalVariablesService.GetValue(GlobalVariableUids.LanguageCode) as string ?? "en";

            try
            {
                var cultureInfo = CultureInfo.GetCultureInfo(systemLanguageCode);
                return Convert.ToString(value, cultureInfo);
            }
            catch (CultureNotFoundException)
            {
                return Convert.ToString(value);
            }
        }

        void TryLoadResource(string uid)
        {
            if (!_storageService.TryReadSerializedValue(out Dictionary<string, string> strings, ResourcesDirectory, uid, StringsFilename))
            {
                strings = new Dictionary<string, string>();
            }

            lock (_resources)
            {
                _resources[uid] = strings ?? new Dictionary<string, string>();
            }
        }

        void TryRegisterDefaultResources(string filename)
        {
            try
            {
                var json = File.ReadAllText(filename);

                // TODO: Refactor to use the resource service.
                var resources =
                    _jsonSerializerService.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
                if (resources == null)
                {
                    return;
                }

                foreach (var defaultResource in resources)
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
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while registering default resources.");
            }
        }

        void Save()
        {
            foreach (var resource in _resources)
            {
                _storageService.WriteSerializedValue(resource.Value, ResourcesDirectory, resource.Key, StringsFilename);
            }

            _logger.LogInformation("Resources written to disk.");
        }
    }
}
