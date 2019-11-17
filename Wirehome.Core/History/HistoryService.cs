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
        private long _componentStatusUpdateDuration;

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

            if (diagnosticsService == null) throw new ArgumentNullException(nameof(diagnosticsService));
            _updateRateCounter = diagnosticsService.CreateOperationsPerSecondCounter("history.update_rate");

            if (systemStatusService == null) throw new ArgumentNullException(nameof(systemStatusService));
            systemStatusService.Set("history.component_status.pending_updates_count", _pendingComponentStatusValues.Count);
            systemStatusService.Set("history.component_status.update_rate", () => _updateRateCounter.Count);
            systemStatusService.Set("history.component_status.update_duration", () => _componentStatusUpdateDuration);
        }

        public void Start()
        {
            _storageService.TryReadOrCreate(out _options, HistoryServiceOptions.Filename);

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

            Task.Run(
                () => ProcessHistoryMessagesAsync(_systemCancellationToken.Token),
                _systemCancellationToken.Token);

            Task.Run(
                () => TryUpdateComponentStatusValuesAsync(_systemCancellationToken.Token),
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

        public Task DeleteComponentStatusHistoryAsync(string componentUid, string statusUid, DateTime? rangeStart, DateTime? rangeEnd, CancellationToken cancellationToken)
        {
            return _repository.DeleteComponentStatusHistoryAsync(componentUid, statusUid, rangeStart, rangeEnd, cancellationToken);
        }

        public Task<int> GetRowCountForComponentStatusHistoryAsync(string componentUid, string statusUid, DateTime? rangeStart, DateTime? rangeEnd, CancellationToken cancellationToken)
        {
            return _repository.GetRowCountForComponentStatusHistoryAsync(componentUid, statusUid, rangeStart, rangeEnd, cancellationToken);
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

        private bool TryInitializeRepository()
        {
            try
            {
                var repository = new HistoryRepository(_storageService);
                repository.Initialize();
                _repository = repository;

                //var repository = new HistoryRepository();
                //repository.Initialize();
                //_repository = repository;
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

        private async Task ProcessHistoryMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await TryProcessNextMessageAsync(cancellationToken).ConfigureAwait(false);
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

        private async Task TryProcessNextMessageAsync(CancellationToken cancellationToken)
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
                _componentStatusUpdateDuration = stopwatch.ElapsedMilliseconds;

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


    //public class HistoryService : IService
    //{
    //    private readonly BlockingCollection<ComponentStatusValue> _pendingComponentStatusValues = new BlockingCollection<ComponentStatusValue>();

    //    private readonly ComponentRegistryService _componentRegistryService;
    //    private readonly StorageService _storageService;
    //    private readonly MessageBusService _messageBusService;
    //    private readonly SystemCancellationToken _systemCancellationToken;
    //    private readonly ILogger _logger;
    //    private readonly OperationsPerSecondCounter _updateRateCounter;

    //    private HistoryRepository _repository;
    //    private HistoryServiceOptions _options;
    //    private long _componentStatusUpdateDuration;

    //    public HistoryService(
    //        ComponentRegistryService componentRegistryService,
    //        StorageService storageService,
    //        MessageBusService messageBusService,
    //        SystemStatusService systemStatusService,
    //        SystemCancellationToken systemCancellationToken,
    //        DiagnosticsService diagnosticsService,
    //        ILogger<HistoryService> logger)
    //    {
    //        _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
    //        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
    //        _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
    //        _systemCancellationToken = systemCancellationToken ?? throw new ArgumentNullException(nameof(systemCancellationToken));
    //        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    //        if (diagnosticsService == null) throw new ArgumentNullException(nameof(diagnosticsService));
    //        _updateRateCounter = diagnosticsService.CreateOperationsPerSecondCounter("history.update_rate");

    //        if (systemStatusService == null) throw new ArgumentNullException(nameof(systemStatusService));
    //        systemStatusService.Set("history.component_status.pending_updates_count", _pendingComponentStatusValues.Count);
    //        systemStatusService.Set("history.component_status.update_rate", () => _updateRateCounter.Count);
    //        systemStatusService.Set("history.component_status.update_duration", () => _componentStatusUpdateDuration);
    //    }

    //    public void Start()
    //    {
    //        _storageService.TryReadOrCreate(out _options, HistoryServiceOptions.Filename);

    //        if (!_options.IsEnabled)
    //        {
    //            _logger.LogInformation("History is disabled.");
    //            return;
    //        }

    //        if (!TryInitializeRepository())
    //        {
    //            return;
    //        }

    //        // Give the pulling code some time to complete before declaring an entity 
    //        // as outdated. 1.25 might be enough additional time.
    //        _repository.ComponentStatusOutdatedTimeout = _options.ComponentStatusPullInterval * 1.25;

    //        AttachToMessageBus();

    //        Task.Factory.StartNew(
    //            () => ProcessHistoryMessages(_systemCancellationToken.Token),
    //            _systemCancellationToken.Token,
    //            TaskCreationOptions.LongRunning,
    //            TaskScheduler.Default);

    //        Task.Run(
    //            () => TryUpdateComponentStatusValuesAsync(_systemCancellationToken.Token),
    //            _systemCancellationToken.Token);
    //    }

    //    public Task<HistoryExtract> BuildHistoryExtractAsync(string componentUid, string statusUid, DateTime rangeStart, DateTime rangeEnd, TimeSpan? interval, HistoryExtractDataType dataType, int maxRowCount, CancellationToken cancellationToken)
    //    {
    //        if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
    //        if (statusUid == null) throw new ArgumentNullException(nameof(statusUid));

    //        if (_repository == null)
    //        {
    //            return null;
    //        }

    //        return new HistoryExtractBuilder(_repository).BuildAsync(componentUid, statusUid, rangeStart, rangeEnd, interval, dataType, maxRowCount, cancellationToken);
    //    }

    //    public Task DeleteComponentStatusHistoryAsync(string componentUid, string statusUid, DateTime? rangeStart, DateTime? rangeEnd, CancellationToken cancellationToken)
    //    {
    //        return _repository.DeleteComponentStatusHistoryAsync(componentUid, statusUid, rangeStart, rangeEnd, cancellationToken);
    //    }

    //    public Task<int> GetRowCountForComponentStatusHistoryAsync(string componentUid, string statusUid, DateTime? rangeStart, DateTime? rangeEnd, CancellationToken cancellationToken)
    //    {
    //        return _repository.GetRowCountForComponentStatusHistoryAsync(componentUid, statusUid, rangeStart, rangeEnd, cancellationToken);
    //    }

    //    private object GetComponentStatusHistorySetting(string componentUid, string statusUid, string settingUid)
    //    {
    //        if (_componentRegistryService.TryGetComponent(componentUid, out var component))
    //        {
    //            if (component.TryGetSetting(settingUid, out var value))
    //            {
    //                return value;
    //            }
    //        }

    //        if (_options.ComponentStatusDefaultSettings.TryGetValue(statusUid, out var defaultSettings))
    //        {
    //            if (defaultSettings.TryGetValue(settingUid, out var value))
    //            {
    //                return value;
    //            }
    //        }

    //        return null;
    //    }

    //    private bool TryInitializeRepository()
    //    {
    //        try
    //        {
    //            var repository = new HistoryRepository();
    //            repository.Initialize();
    //            _repository = repository;

    //            return true;
    //        }
    //        catch (Exception exception)
    //        {
    //            _logger.Log(LogLevel.Warning, exception, "Error while initializing history repository.");
    //            return false;
    //        }
    //    }

    //    private void AttachToMessageBus()
    //    {
    //        var filter = new WirehomeDictionary()
    //            .WithValue("type", "component_registry.event.status_changed");

    //        _messageBusService.Subscribe("history_receiver", filter, OnComponentStatusChanged);
    //    }

    //    private void OnComponentStatusChanged(MessageBusMessage busMessage)
    //    {
    //        try
    //        {
    //            var message = busMessage.Message;

    //            TryEnqueueComponentStatusValue(
    //                Convert.ToString(message["component_uid"], CultureInfo.InvariantCulture),
    //                Convert.ToString(message["status_uid"], CultureInfo.InvariantCulture),
    //                message.GetValueOrDefault("new_value", null),
    //                DateTime.UtcNow);
    //        }
    //        catch (OperationCanceledException)
    //        {
    //        }
    //        catch (Exception exception)
    //        {
    //            _logger.LogError(exception, "Error while processing changed component status.");
    //        }
    //    }

    //    private void ProcessHistoryMessages(CancellationToken cancellationToken)
    //    {
    //        try
    //        {
    //            Thread.CurrentThread.Name = nameof(ProcessHistoryMessages);

    //            while (!cancellationToken.IsCancellationRequested)
    //            {
    //                TryProcessNextMessage(cancellationToken);
    //            }
    //        }
    //        catch (OperationCanceledException)
    //        {
    //        }
    //        catch (Exception exception)
    //        {
    //            _logger.Log(LogLevel.Error, exception, "Error while processing messages.");
    //        }
    //    }

    //    private void TryProcessNextMessage(CancellationToken cancellationToken)
    //    {
    //        try
    //        {
    //            var componentStatusValue = _pendingComponentStatusValues.Take(cancellationToken);
    //            if (componentStatusValue == null || cancellationToken.IsCancellationRequested)
    //            {
    //                return;
    //            }

    //            var stopwatch = Stopwatch.StartNew();

    //            var roundSetting = GetComponentStatusHistorySetting(componentStatusValue.ComponentUid,
    //                componentStatusValue.StatusUid, HistorySettingName.RoundDigits);
    //            if (roundSetting != null)
    //            {
    //                var roundDigitsCount = Convert.ToInt32(roundSetting);
    //                if (decimal.TryParse(componentStatusValue.Value, out var @decimal))
    //                {
    //                    @decimal = Math.Round(@decimal, roundDigitsCount);
    //                    componentStatusValue.Value = Convert.ToString(@decimal, CultureInfo.InvariantCulture);
    //                }
    //            }

    //            _repository.UpdateComponentStatusValueAsync(componentStatusValue, cancellationToken).GetAwaiter().GetResult();

    //            stopwatch.Stop();
    //            _componentStatusUpdateDuration = stopwatch.ElapsedMilliseconds;

    //            _updateRateCounter.Increment();
    //        }
    //        catch (OperationCanceledException)
    //        {
    //        }
    //        catch (Exception exception)
    //        {
    //            _logger.Log(LogLevel.Error, exception, "Error while processing message.");
    //        }
    //    }

    //    private async Task TryUpdateComponentStatusValuesAsync(CancellationToken cancellationToken)
    //    {
    //        while (!cancellationToken.IsCancellationRequested)
    //        {
    //            try
    //            {
    //                await Task.Delay(_options.ComponentStatusPullInterval, cancellationToken).ConfigureAwait(false);

    //                foreach (var component in _componentRegistryService.GetComponents())
    //                {
    //                    foreach (var status in component.GetStatus())
    //                    {
    //                        TryEnqueueComponentStatusValue(
    //                            component.Uid,
    //                            status.Key,
    //                            status.Value,
    //                            DateTime.UtcNow);
    //                    }
    //                }
    //            }
    //            catch (OperationCanceledException)
    //            {
    //            }
    //            catch (Exception exception)
    //            {
    //                _logger.Log(LogLevel.Error, exception, "Error while updating component property values.");
    //            }
    //        }
    //    }

    //    private void TryEnqueueComponentStatusValue(
    //        string componentUid,
    //        string statusUid,
    //        object value,
    //        DateTime timestamp)
    //    {
    //        try
    //        {
    //            if (IsComponentStatusBlacklisted(componentUid, statusUid))
    //            {
    //                return;
    //            }

    //            var componentStatusValue = new ComponentStatusValue
    //            {
    //                ComponentUid = componentUid,
    //                StatusUid = statusUid,
    //                Value = Convert.ToString(value, CultureInfo.InvariantCulture),
    //                Timestamp = timestamp
    //            };

    //            _pendingComponentStatusValues.Add(componentStatusValue);
    //        }
    //        catch (OperationCanceledException)
    //        {
    //        }
    //        catch (Exception exception)
    //        {
    //            _logger.LogError(exception, $"Error while enque component status value '{componentUid}.{statusUid}'.");
    //        }
    //    }

    //    private bool IsComponentStatusBlacklisted(string componentUid, string statusUid)
    //    {
    //        if (_options.ComponentBlacklist?.Contains(componentUid) == true)
    //        {
    //            return true;
    //        }

    //        if (_options.ComponentStatusBlacklist?.Contains(statusUid) == true)
    //        {
    //            return true;
    //        }

    //        if (_options.FullComponentStatusBlacklist?.Contains(componentUid + "." + statusUid) == true)
    //        {
    //            return true;
    //        }

    //        return false;
    //    }
    //}
}
