using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Wirehome.Cloud.Services.Repository.Models;
using Wirehome.Core.Storage;

namespace Wirehome.Cloud.Services.Repository
{
    public class RepositoryService
    {
        private readonly Dictionary<string, IdentityConfiguration> _identityConfigurationsCache = new Dictionary<string, IdentityConfiguration>();
        private readonly string _rootPath;
        private readonly ILogger _logger;
        
        public RepositoryService(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<RepositoryService>();

            if (Debugger.IsAttached)
            {
                _rootPath = Path.Combine(Environment.ExpandEnvironmentVariables("%APPDATA%"), "Wirehome.Cloud", "Identities");
            }
            else
            {
                _rootPath = "D:/home/data/Wirehome.Cloud/Identities";
            }
        }

        public bool TryGetIdentityConfiguration(string identityUid, out IdentityConfiguration identityConfiguration)
        {
            if (identityUid == null) throw new ArgumentNullException(nameof(identityUid));

            lock (_identityConfigurationsCache)
            {
                if (_identityConfigurationsCache.TryGetValue(identityUid, out identityConfiguration))
                {
                    return true;
                }

                var filename = Path.Combine(_rootPath, identityUid, DefaultFilenames.Configuration);
                if (!File.Exists(filename))
                {
                    identityConfiguration = null;
                    return false;
                }

                try
                {
                    var json = File.ReadAllText(filename, Encoding.UTF8);
                    identityConfiguration = JsonConvert.DeserializeObject<IdentityConfiguration>(json);

                    _identityConfigurationsCache[identityUid] = identityConfiguration;
                    return true;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, $"Error while loading file '{filename}'.");

                    identityConfiguration = null;
                    return false;
                }
            }
        }
    }
}
