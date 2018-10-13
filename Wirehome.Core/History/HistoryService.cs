using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components;
using Wirehome.Core.Diagnostics;
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

        private HistoryRepository _repository;

        public HistoryService(
            ComponentRegistryService componentRegistryService,
            StorageService storageService,
            MessageBusService messageBusService,
            SystemStatusService systemStatusService,
            SystemService systemService,
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
        }

        public void Start()
        {
            if (!TryInitializeRepository())
            {
                return;
            }

            AttachToMessageBus();

            Task.Factory.StartNew(
                () => TryProcessMessages(_systemService.CancellationToken), 
                _systemService.CancellationToken, 
                TaskCreationOptions.LongRunning, 
                TaskScheduler.Default);

            Task.Run(
                () => TryUpdateComponentPropertyValuesAsync(_systemService.CancellationToken),
                _systemService.CancellationToken);
        }

        private void AttachToMessageBus()
        {
            var filter = new WirehomeDictionary().WithType("component_registry.event.status_reported");
            _messageBusService.Subscribe("history_receiver", filter, OnComponentPropertyReported);
        }

        private void OnComponentPropertyReported(WirehomeDictionary properties)
        {
            var componentStatusValue = new ComponentStatusValue
            {
                ComponentUid = Convert.ToString(properties["component_uid"], CultureInfo.InvariantCulture),
                StatusUid = Convert.ToString(properties["status_uid"], CultureInfo.InvariantCulture),
                Value = Convert.ToString(properties.GetValueOrDefault("new_value", null), CultureInfo.InvariantCulture),
                Timestamp = (DateTime)properties.GetValueOrDefault("timestamp", DateTime.UtcNow)
            };

            _pendingComponentStatusValues.Add(componentStatusValue, _systemService.CancellationToken);
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
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, exception, "Error while processing message.");
            }
        }

        private async Task TryUpdateComponentPropertyValuesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    foreach (var component in _componentRegistryService.GetComponents())
                    {
                        foreach (var status in component.Status)
                        {
                            var componentStatusValue = new ComponentStatusValue
                            {
                                ComponentUid = component.Uid,
                                StatusUid = status.Key,
                                Timestamp = DateTimeOffset.UtcNow,
                                Value = Convert.ToString(status.Value, CultureInfo.InvariantCulture)
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
                finally
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                }
            }
        }
    }
}
