using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.History.Extract;
using Wirehome.Core.History.Repository;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Model;
using Wirehome.Core.Storage;
using Wirehome.Core.System;

namespace Wirehome.Core.History
{
    public class HistoryService
    {
        private readonly BlockingCollection<ComponentStatusValue> _pendingComponentStatusValues = new BlockingCollection<ComponentStatusValue>();

        private readonly ComponentRegistryService _componentRegistryService;
        private readonly StorageService _storageService;
        private readonly MessageBusService _messageBusService;
        private readonly SystemService _systemService;
        private readonly ILogger _logger;
        private readonly OperationsPerSecondCounter _updateRateCounter;

        private HistoryRepository _repository;
        private HistorySettings _settings;

        public HistoryService(
            ComponentRegistryService componentRegistryService,
            StorageService storageService,
            MessageBusService messageBusService,
            SystemStatusService systemStatusService,
            SystemService systemService,
            DiagnosticsService diagnosticsService,
            ILoggerFactory loggerFactory)
        {
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _systemService = systemService ?? throw new ArgumentNullException(nameof(systemService));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<HistoryService>();

            if (systemStatusService == null) throw new ArgumentNullException(nameof(systemStatusService));
            systemStatusService.Set("history.pending_component_status_values_count", _pendingComponentStatusValues.Count);

            if (diagnosticsService == null) throw new ArgumentNullException(nameof(diagnosticsService));
            _updateRateCounter = diagnosticsService.CreateOperationsPerSecondCounter("history.update_rate");
            systemStatusService.Set("history.update_rate", () => _updateRateCounter.Count);
        }

        public void Start()
        {
            if (!_storageService.TryRead(out _settings, "HistorySettings.json"))
            {
                _settings = new HistorySettings();
            }

            if (!_settings.IsEnabled)
            {
                _logger.LogInformation("History is disabled.");
                return;
            }

            if (!TryInitializeRepository())
            {
                return;
            }

            // Give the pulling code some time to complete before declaring an entity 
            // as outdated. 1.25 might be enough additional time.
            _repository.ComponentStatusOutdatedTimeout = _settings.ComponentStatusPullInterval * 1.25;

            AttachToMessageBus();

            Task.Factory.StartNew(
                () => TryProcessMessages(_systemService.CancellationToken),
                _systemService.CancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            Task.Run(
                () => TryUpdateComponentStatusValuesAsync(_systemService.CancellationToken),
                _systemService.CancellationToken);
        }

        public HistoryExtract BuildHistoryExtract(string componentUid, string statusUid, DateTime rangeStart, DateTime rangeEnd, TimeSpan interval, HistoryExtractDataType dataType)
        {
            var historyExtract = new HistoryExtractBuilder(_repository).Build(componentUid, statusUid, rangeStart, rangeEnd, interval, dataType);
            for (var i = historyExtract.DataPoints.Count - 1; i > 0; i--)
            {

            }

            return historyExtract;
        }

        private bool TryInitializeRepository()
        {
            try
            {
                var repository = new HistoryRepository();
                repository.Initialize();
                _repository = repository;

                return true;
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Warning, exception, "Error while initializing history repository.");
                return false;
            }
        }

        private void AttachToMessageBus()
        {
            var filter = new WirehomeDictionary()
                .WithValue("type", "component_registry.event.status_changed");

            _messageBusService.Subscribe("history_receiver", filter, OnComponentStatusChanged);
        }

        private void OnComponentStatusChanged(WirehomeDictionary message)
        {
            try
            {
                TryEnqueueComponentStatusValue(
                    Convert.ToString(message["component_uid"], CultureInfo.InvariantCulture),
                    Convert.ToString(message["status_uid"], CultureInfo.InvariantCulture),
                    message.GetValueOrDefault("new_value", null),
                    DateTime.UtcNow);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while processing changed component status.");
            }
        }

        private void TryProcessMessages(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TryProcessNextMessage(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, exception, "Error while processing messages.");
            }
        }

        private void TryProcessNextMessage(CancellationToken cancellationToken)
        {
            try
            {
                var componentStatusValue = _pendingComponentStatusValues.Take(cancellationToken);
                if (componentStatusValue == null || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                _repository.UpdateComponentStatusValue(componentStatusValue);
                _updateRateCounter.Increment();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, exception, "Error while processing message.");
            }
        }

        private async Task TryUpdateComponentStatusValuesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_settings.ComponentStatusPullInterval, cancellationToken).ConfigureAwait(false);

                    foreach (var component in _componentRegistryService.GetComponents())
                    {
                        foreach (var status in component.Status)
                        {
                            TryEnqueueComponentStatusValue(
                                component.Uid,
                                status.Key,
                                status.Value,
                                DateTime.UtcNow);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    _logger.Log(LogLevel.Error, exception, "Error while updating component property values.");
                }
            }
        }

        private void TryEnqueueComponentStatusValue(
            string componentUid,
            string statusUid,
            object value,
            DateTime timestamp)
        {
            try
            {
                var stringValue = Convert.ToString(value, CultureInfo.InvariantCulture);

                var roundSetting = _componentRegistryService.GetComponentSetting(componentUid, "history.round_digits");
                if (roundSetting != null)
                {
                    var roundDigitsCount = Convert.ToInt32(roundSetting);
                    if (decimal.TryParse(stringValue, out var @decimal))
                    {
                        @decimal = Math.Round(@decimal, roundDigitsCount);
                        stringValue = Convert.ToString(@decimal, CultureInfo.InvariantCulture);
                    }
                }

                var componentStatusValue = new ComponentStatusValue
                {
                    ComponentUid = componentUid,
                    StatusUid = statusUid,
                    Value = stringValue,
                    Timestamp = timestamp
                };

                _pendingComponentStatusValues.Add(componentStatusValue);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while enque component status value '{componentUid}.{statusUid}'.");
            }
        }
    }
}
