using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.History.Extract;
using Wirehome.Core.History.Repository;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Model;
using Wirehome.Core.Storage;
using Wirehome.Core.System;

namespace Wirehome.Core.History
{
    public class HistoryService : IService
    {
        private readonly BlockingCollection<ComponentStatusValue> _pendingComponentStatusValues = new BlockingCollection<ComponentStatusValue>();

        private readonly ComponentRegistryService _componentRegistryService;
        private readonly StorageService _storageService;
        private readonly MessageBusService _messageBusService;
        private readonly SystemCancellationToken _systemCancellationToken;
        private readonly ILogger _logger;

        private readonly OperationsPerSecondCounter _updateRateCounter;

        private HistoryRepository _repository;
        private HistoryServiceOptions _options;

        private long _lastUpdateDuration;
        private long _maxUpdateDuration;
        private long? _minUpdateDuration;
        private long _updatesCount;
        private long _totalUpdateDuration;

        public HistoryService(
            ComponentRegistryService componentRegistryService,
            StorageService storageService,
            MessageBusService messageBusService,
            SystemStatusService systemStatusService,
            SystemCancellationToken systemCancellationToken,
            DiagnosticsService diagnosticsService,
            ILogger<HistoryService> logger)
        {
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _systemCancellationToken = systemCancellationToken ?? throw new ArgumentNullException(nameof(systemCancellationToken));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _repository = new HistoryRepository(_storageService);

            if (diagnosticsService == null) throw new ArgumentNullException(nameof(diagnosticsService));
            _updateRateCounter = diagnosticsService.CreateOperationsPerSecondCounter("history.update_rate");

            if (systemStatusService == null) throw new ArgumentNullException(nameof(systemStatusService));
            systemStatusService.Set("history.component_status.pending_updates_count", _pendingComponentStatusValues.Count);
            
            systemStatusService.Set("history.component_status.update_rate", () => _updateRateCounter.Count);
            systemStatusService.Set("history.component_status.updates_count", () => _updatesCount);
            systemStatusService.Set("history.component_status.last_update_duration", () => _lastUpdateDuration);
            systemStatusService.Set("history.component_status.max_update_duration", () => _maxUpdateDuration);
            systemStatusService.Set("history.component_status.min_update_duration", () => _minUpdateDuration);
            systemStatusService.Set("history.component_status.average_update_duration", () => _totalUpdateDuration / (decimal)_updatesCount);
        }

        public void Start()
        {
            _storageService.TryReadOrCreate(out _options, HistoryServiceOptions.Filename);

            if (!_options.IsEnabled)
            {
                _logger.LogInformation("History is disabled.");
                return;
            }
        
            // Give the pulling code some time to complete before declaring an entity 
            // as outdated. 1.25 might be enough additional time.
            _repository.ComponentStatusOutdatedTimeout = _options.ComponentStatusPullInterval * 1.25;

            AttachToMessageBus();

            Task.Run(
                () => TryProcesstMessages(_systemCancellationToken.Token),
                _systemCancellationToken.Token);

            Task.Run(
                () => TryUpdateComponentStatusValues(_systemCancellationToken.Token),
                _systemCancellationToken.Token);
        }

        public Task<HistoryExtract> BuildHistoryExtractAsync(string componentUid, string statusUid, DateTime rangeStart, DateTime rangeEnd, TimeSpan? interval, HistoryExtractDataType dataType, int maxRowCount, CancellationToken cancellationToken)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (statusUid == null) throw new ArgumentNullException(nameof(statusUid));

            if (_repository == null)
            {
                return null;
            }

            return new HistoryExtractBuilder(_repository).BuildAsync(componentUid, statusUid, rangeStart, rangeEnd, interval, dataType, maxRowCount, cancellationToken);
        }

        public void ResetStatistics()
        {
            _maxUpdateDuration = 0L;
            _minUpdateDuration = null;
            _updatesCount = 0L;
            _totalUpdateDuration = 0L;
        }

        public Task<List<HistoryValueElement>> GetComponentStatusValues(string componentUid, string statusUid, DateTime day, CancellationToken cancellationToken)
        {
            return _repository.GetComponentStatusValues(new ComponentStatusFilter()
            {
                ComponentUid = componentUid,
                StatusUid = statusUid,
                RangeStart = new DateTime(day.Year, day.Month, day.Day, 0, 0, 0),
                RangeEnd = new DateTime(day.Year, day.Month, day.Day, 23, 59, 59),
                MaxEntityCount = null
            }, cancellationToken);
        }

        public Task DeleteComponentStatusHistory(string componentUid, string statusUid, CancellationToken cancellationToken)
        {
            return _repository.DeleteComponentStatusHistory(componentUid, statusUid, cancellationToken);
        }

        public Task DeleteComponentHistory(string componentUid, CancellationToken cancellationToken)
        {
            return _repository.DeleteComponentHistory(componentUid, cancellationToken);
        }

        public Task<long> GetComponentStatusHistorySize(string componentUid, string statusUid, CancellationToken cancellationToken)
        {
            return _repository.GetComponentStatusHistorySize(componentUid, statusUid, cancellationToken);
        }

        public Task<long> GetComponentHistorySize(string componentUid, CancellationToken cancellationToken)
        {
            return _repository.GetComponentHistorySize(componentUid, cancellationToken);
        }

        private object GetComponentStatusHistorySetting(string componentUid, string statusUid, string settingUid)
        {
            if (_componentRegistryService.TryGetComponent(componentUid, out var component))
            {
                if (component.TryGetSetting(settingUid, out var value))
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

        private async Task TryProcesstMessages(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await TryProcessNextMessage(cancellationToken).ConfigureAwait(false);
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

        private async Task TryProcessNextMessage(CancellationToken cancellationToken)
        {
            try
            {
                var componentStatusValue = _pendingComponentStatusValues.Take(cancellationToken);
                if (componentStatusValue == null || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var stopwatch = Stopwatch.StartNew();

                var roundSetting = GetComponentStatusHistorySetting(componentStatusValue.ComponentUid,
                    componentStatusValue.StatusUid, HistorySettingName.RoundDigits);

                if (roundSetting != null)
                {
                    var roundDigitsCount = Convert.ToInt32(roundSetting);
                    if (decimal.TryParse(componentStatusValue.Value, out var @decimal))
                    {
                        @decimal = Math.Round(@decimal, roundDigitsCount);
                        componentStatusValue.Value = Convert.ToString(@decimal, CultureInfo.InvariantCulture);
                    }
                }

                await _repository.UpdateComponentStatusValueAsync(componentStatusValue, cancellationToken).ConfigureAwait(false);

                stopwatch.Stop();

                var duration = stopwatch.ElapsedMilliseconds;

                _totalUpdateDuration += duration;
                _updatesCount++;

                if (duration > _maxUpdateDuration)
                {
                    _maxUpdateDuration = duration;
                }

                if (!_minUpdateDuration.HasValue || duration < _minUpdateDuration)
                {
                    _minUpdateDuration = duration;
                }

                _lastUpdateDuration = duration;

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

        private async Task TryUpdateComponentStatusValues(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_options.ComponentStatusPullInterval, cancellationToken).ConfigureAwait(false);

                    foreach (var component in _componentRegistryService.GetComponents())
                    {
                        foreach (var status in component.GetStatus())
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
                _logger.LogError(exception, $"Error while enqueue component status value '{componentUid}.{statusUid}'.");
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
    }
}
