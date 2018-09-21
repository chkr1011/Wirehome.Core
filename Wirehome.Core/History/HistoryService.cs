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
        private readonly BlockingCollection<ComponentStatusValueMessage> _pendingComponentStatusChangedMessages = new BlockingCollection<ComponentStatusValueMessage>();
        
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
            systemStatusService.Set("history.pending_component_status_changed_messages", _pendingComponentStatusChangedMessages.Count);
        }

        public void Start()
        {
            if (!TryInitializeRepository())
            {
                return;
            }

            AttachToMessageBus();

            Task.Factory.StartNew(() => TryProcessMessages(_systemService.CancellationToken), _systemService.CancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void AttachToMessageBus()
        {
            _messageBusService.Subscribe(Guid.NewGuid().ToString("D"), new WirehomeDictionary().WithType("component_registry.event.component_status_reported"), OnComponentPropertyReported);
        }

        private void OnComponentPropertyReported(WirehomeDictionary properties)
        {
            var message = new ComponentStatusValueMessage
            {
                ComponentUid = properties.GetValueOrDefault("component_uid") as string,
                StatusUid = properties.GetValueOrDefault("status_uid") as string,
                Value = properties.GetValueOrDefault("new_value"),
                Timestamp = (DateTime)properties.GetValueOrDefault("timestamp")
            };

            _pendingComponentStatusChangedMessages.Add(message, _systemService.CancellationToken);
        }

        private bool TryInitializeRepository()
        {
            try
            {
                _repository = new HistoryRepository(_storageService);
                _repository.Initialize();

                return true;
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Warning, exception, "Error while initializing history repository.");

                _repository = null;
            }

            return false;
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
                var message = _pendingComponentStatusChangedMessages.Take(cancellationToken);
                if (message == null)
                {
                    return;
                }

                var now = DateTime.Now;
                
                var existingRow = _repository.GetComponentStatusRow(message.ComponentUid, message.StatusUid);
                if (existingRow?.Timestamp > message.Timestamp)
                {
                    return;
                }

                var newValueText = Convert.ToString(message.Value, CultureInfo.InvariantCulture);
                var valueHasChanged = !string.Equals(existingRow?.Value, newValueText, StringComparison.Ordinal);

                if (existingRow != null)
                {
                    _repository.UpdateComponentStatusRow(existingRow.ID, now);
                }
                
                if (valueHasChanged)
                {
                    _repository.InsertComponentStatusRow(message.ComponentUid, message.StatusUid, newValueText, now);    
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, exception, "Error while processing message.");
            }
        }

        private void TryUpdateComponentPropertyValues(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                        
                    //_repository.
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
