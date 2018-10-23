using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        private HistoryServiceOptions _options;
        private long _componentStatusUpateDuration;

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

            if (diagnosticsService == null) throw new ArgumentNullException(nameof(diagnosticsService));
            _updateRateCounter = diagnosticsService.CreateOperationsPerSecondCounter("history.update_rate");

            if (systemStatusService == null) throw new ArgumentNullException(nameof(systemStatusService));
            systemStatusService.Set("history.component_status.pending_updates_count", _pendingComponentStatusValues.Count);
            systemStatusService.Set("history.component_status.update_rate", () => _updateRateCounter.Count);
            systemStatusService.Set("history.component_status.update_duration", () => _componentStatusUpateDuration);
        }

        public void Start()
        {
            _storageService.TryReadOrCreate(out _options, "HistoryServiceConfiguration.json");

            if (!_options.IsEnabled)
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
            _repository.ComponentStatusOutdatedTimeout = _options.ComponentStatusPullInterval * 1.25;

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

        private void OnComponentStatusChanged(MessageBusMessage busMessage)
        {
            try
            {
                var message = busMessage.Message;

                TryEnqueueComponentStatusValue(
                    Convert.ToString(message["component_uid"], CultureInfo.InvariantCulture),
                    Convert.ToString(message["status_uid"], CultureInfo.InvariantCulture),
                    message.GetValueOrDefault("new_value", null),
                    DateTime.UtcNow);
            }
            catch (OperationCanceledException)
            {
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

                var stopwatch = Stopwatch.StartNew();

                var roundSetting = GetComponentStatusHistorySetting(componentStatusValue.ComponentUid, componentStatusValue.StatusUid, HistorySettingName.RoundDigits);
                if (roundSetting != null)
                {
                    var roundDigitsCount = Convert.ToInt32(roundSetting);
                    if (decimal.TryParse(componentStatusValue.Value, out var @decimal))
                    {
                        @decimal = Math.Round(@decimal, roundDigitsCount);
                        componentStatusValue.Value = Convert.ToString(@decimal, CultureInfo.InvariantCulture);
                    }
                }

                _repository.UpdateComponentStatusValue(componentStatusValue);

                stopwatch.Stop();
                _componentStatusUpateDuration = stopwatch.ElapsedMilliseconds;

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
                    await Task.Delay(_options.ComponentStatusPullInterval, cancellationToken).ConfigureAwait(false);

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
                if (IsComponentStatusBlacklisted(componentUid, statusUid))
                {
                    return;
                }

                var componentStatusValue = new ComponentStatusValue
                {
                    ComponentUid = componentUid,
                    StatusUid = statusUid,
                    Value = Convert.ToString(value, CultureInfo.InvariantCulture),
                    Timestamp = timestamp
                };

                _pendingComponentStatusValues.Add(componentStatusValue);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while enque component status value '{componentUid}.{statusUid}'.");
            }
        }

        private bool IsComponentStatusBlacklisted(string componentUid, string statusUid)
        {
            if (_options.ComponentBlacklist?.Contains(componentUid) == true)
            {
                return true;
            }

            if (_options.ComponentStatusBlacklist?.Contains(statusUid) == true)
            {
                return true;
            }

            if (_options.FullComponentStatusBlacklist?.Contains(componentUid + "." + statusUid) == true)
            {
                return true;
            }

            return false;
        }

        private object GetComponentStatusHistorySetting(string componentUid, string statusUid, string settingUid)
        {
            if (_componentRegistryService.TryGetComponent(componentUid, out var component))
            {
                if (component.Settings.TryGetValue(settingUid, out var value))
                {
                    return value;
                }
            }

            if (_options.ComponentStatusDefaultSettings.TryGetValue(statusUid, out var defaultSettings))
            {
                if (defaultSettings.TryGetValue(settingUid, out var value))
                {
                    return value;
                }
            }

            return null;
        }
    }
}
