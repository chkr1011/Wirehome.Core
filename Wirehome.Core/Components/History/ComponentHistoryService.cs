using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.History;
using Wirehome.Core.Storage;
using Wirehome.Core.System;

namespace Wirehome.Core.Components.History
{
    public class ComponentHistoryService : IService
    {
        readonly BlockingCollection<ComponentStatusHistoryWorkItem> _pendingStatusWorkItems = new BlockingCollection<ComponentStatusHistoryWorkItem>();
        readonly HistoryService _historyService;
        readonly StorageService _storageService;
        readonly SystemCancellationToken _systemCancellationToken;
        readonly ILogger<ComponentHistoryService> _logger;

        ComponentHistoryServiceOptions _options;

        public ComponentHistoryService(
            HistoryService historyService,
            StorageService storageService,
            SystemStatusService systemStatusService,
            SystemCancellationToken systemCancellationToken,
            ILogger<ComponentHistoryService> logger)
        {
            _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _systemCancellationToken = systemCancellationToken ?? throw new ArgumentNullException(nameof(systemCancellationToken));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (systemStatusService == null) throw new ArgumentNullException(nameof(systemStatusService));
            systemStatusService.Set("component_history.pending_status_work_items", _pendingStatusWorkItems.Count);
        }

        public void Start()
        {
            _storageService.TryReadOrCreate(out _options, ComponentHistoryServiceOptions.Filename);
            if (!_options.IsEnabled)
            {
                _logger.LogInformation("Component history is disabled.");
                return;
            }

            Task.Run(() => TryProcessWorkItems(_systemCancellationToken.Token), _systemCancellationToken.Token);
            Task.Run(() => TryUpdateComponentStatusValues(_systemCancellationToken.Token), _systemCancellationToken.Token);
        }

        public Func<List<Component>> ComponentsProvider { get; set; }

        public string BuildComponentHistoryPath(string componentUid)
        {
            return Path.Combine(_storageService.DataPath, "Components", componentUid, "History");
        }

        public string BuildComponentStatusHistoryPath(string componentUid, string statusUid)
        {
            return Path.Combine(BuildComponentHistoryPath(componentUid), "Status", statusUid);
        }

        public void OnComponentStatusChanged(Component component, string statusUid, object newValue)
        {
            try
            {
                TryEnqueueComponentStatusValue(
                    component,
                    statusUid,
                    newValue,
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

        async Task TryProcessWorkItems(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await TryProcessNextWorkItem(cancellationToken).ConfigureAwait(false);
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

        async Task TryProcessNextWorkItem(CancellationToken cancellationToken)
        {
            try
            {
                var statusWorkItem = _pendingStatusWorkItems.Take(cancellationToken);
                if (statusWorkItem == null || cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                
                HistoryValueFormatterOptions formatterOptions = null;

                _options.ComponentStatusDefaultSettings.TryGetValue(statusWorkItem.StatusUid, out var defaultSettings);

                var formatterOptionsFactory = new HistoryValueFormatterOptionsFactory();
                formatterOptions = formatterOptionsFactory.Create(statusWorkItem.Component.GetSettings(), defaultSettings);

                await _historyService.Update(new HistoryUpdateOperation
                {
                    Path = BuildComponentStatusHistoryPath(statusWorkItem.Component.Uid, statusWorkItem.StatusUid),
                    Timestamp = statusWorkItem.Timestamp,
                    Value = statusWorkItem.Value,
                    ValueFormatterOptions = formatterOptions,
                    // Give the pulling code some time to complete before declaring an entity
                    // as outdated. 1.25 might be enough additional time.
                    OldValueTimeToLive = _options.ComponentStatusPullInterval * 1.25
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, exception, "Error while processing message.");
            }
        }

        async Task TryUpdateComponentStatusValues(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_options.ComponentStatusPullInterval, cancellationToken).ConfigureAwait(false);

                    foreach (var component in ComponentsProvider())
                    {
                        foreach (var status in component.GetStatus())
                        {
                            TryEnqueueComponentStatusValue(
                                component,
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

        void TryEnqueueComponentStatusValue(
            Component component,
            string statusUid,
            object value,
            DateTime timestamp)
        {
            try
            {
                if (IsComponentStatusBlacklisted(component.Uid, statusUid))
                {
                    return;
                }

                var statusWorkItem = new ComponentStatusHistoryWorkItem
                {
                    Component = component,
                    StatusUid = statusUid,
                    Value = value,
                    Timestamp = timestamp
                };

                _pendingStatusWorkItems.Add(statusWorkItem);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while enqueue component status value '{component.Uid}.{statusUid}'.");
            }
        }

        bool IsComponentStatusBlacklisted(string componentUid, string statusUid)
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
