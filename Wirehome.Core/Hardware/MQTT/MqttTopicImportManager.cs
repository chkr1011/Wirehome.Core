using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Foundation;

namespace Wirehome.Core.Hardware.MQTT
{
    public sealed class MqttTopicImportManager : IDisposable
    {
        readonly Dictionary<string, MqttTopicImporter> _importers = new();
        readonly AsyncLock _importersLock = new();
        readonly ILogger _logger;

        readonly MqttService _mqttService;

        public MqttTopicImportManager(MqttService mqttService, ILogger logger)
        {
            _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Dispose()
        {
            _importersLock.Dispose();
        }

        public List<string> GetTopicImportUids()
        {
            _importersLock.Enter();
            try
            {
                return _importers.Select(i => i.Key).ToList();
            }
            finally
            {
                _importersLock.Exit();
            }
        }

        public async Task<string> StartTopicImport(string uid, MqttImportTopicParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (string.IsNullOrEmpty(uid))
            {
                uid = Guid.NewGuid().ToString("D");
            }

            var importer = new MqttTopicImporter(parameters, _mqttService, _logger);
            await importer.Start().ConfigureAwait(false);

            await _importersLock.EnterAsync().ConfigureAwait(false);
            try
            {
                if (_importers.TryGetValue(uid, out var existingImporter))
                {
                    await existingImporter.Stop().ConfigureAwait(false);
                }

                _importers[uid] = importer;
            }
            finally
            {
                _importersLock.Exit();
            }

            _logger.Log(LogLevel.Information, "Started importer '{0}' for topic '{1}' from server '{2}'.", uid, parameters.Topic, parameters.Server);
            return uid;
        }

        public async Task StopTopicImport(string uid)
        {
            if (uid == null)
            {
                throw new ArgumentNullException(nameof(uid));
            }

            await _importersLock.EnterAsync().ConfigureAwait(false);
            try
            {
                if (_importers.TryGetValue(uid, out var importer))
                {
                    await importer.Stop().ConfigureAwait(false);
                    _logger.Log(LogLevel.Information, "Stopped importer '{0}'.", uid);
                }

                _importers.Remove(uid);
            }
            finally
            {
                _importersLock.Exit();
            }
        }
    }
}