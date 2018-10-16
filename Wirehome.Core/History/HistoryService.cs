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

            _repository.ComponentStatusPullInterval = _settings.ComponentStatusPullInterval;

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
            return new HistoryExtractBuilder(_repository).Build(componentUid, statusUid, rangeStart, rangeEnd, interval, dataType);
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
                var componentStatusValue = new ComponentStatusValue
                {
                    ComponentUid = Convert.ToString(message["component_uid"], CultureInfo.InvariantCulture),
                    StatusUid = Convert.ToString(message["status_uid"], CultureInfo.InvariantCulture),
                    Value = Convert.ToString(message.GetValueOrDefault("new_value", null), CultureInfo.InvariantCulture),
                    Timestamp = DateTime.UtcNow
                };

                _pendingComponentStatusValues.Add(componentStatusValue, _systemService.CancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while processing changed component status.");
            }
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
                    await Task.Delay(_settings.ComponentStatusPullInterval, cancellationToken);

                    foreach (var component in _componentRegistryService.GetComponents())
                    {
                        foreach (var status in component.Status)
                        {
                            var componentStatusValue = new ComponentStatusValue
                            {
                                ComponentUid = component.Uid,
                                StatusUid = status.Key,
                                Value = Convert.ToString(status.Value, CultureInfo.InvariantCulture),
                                Timestamp = DateTime.UtcNow
                            };

                            _pendingComponentStatusValues.Add(componentStatusValue, cancellationToken);
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
    }
}
