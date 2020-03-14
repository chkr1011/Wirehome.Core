using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wirehome.Core.Backup;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.History;
using Wirehome.Core.Storage;
using Wirehome.Core.System;

namespace Wirehome.Core.Components.History
{
    public sealed class ComponentHistoryService : IService, IDisposable
    {
        readonly BlockingCollection<ComponentStatusHistoryWorkItem> _pendingStatusWorkItems = new BlockingCollection<ComponentStatusHistoryWorkItem>();
        readonly ComponentRegistryService _componentRegistryService;
        readonly HistoryService _historyService;
        readonly StorageService _storageService;
        readonly SystemCancellationToken _systemCancellationToken;
        private readonly BackupService _backupService;
        readonly ILogger<ComponentHistoryService> _logger;

        ComponentHistoryServiceOptions _options;

        public ComponentHistoryService(
            ComponentRegistryService componentRegistryService,
            HistoryService historyService,
            StorageService storageService,
            SystemStatusService systemStatusService,
            SystemCancellationToken systemCancellationToken,
            BackupService backupService,
            ILogger<ComponentHistoryService> logger)
        {
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
            _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _systemCancellationToken = systemCancellationToken ?? throw new ArgumentNullException(nameof(systemCancellationToken));
            _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (systemStatusService == null) throw new ArgumentNullException(nameof(systemStatusService));
            systemStatusService.Set("component_history.pending_status_work_items", _pendingStatusWorkItems.Count);

            _componentRegistryService.ComponentStatusChanged += OnComponentStatusChanged;
        }

        public void Start()
        {
            _backupService.ExcludePathFromBackup(Path.Combine(_storageService.DataPath, "History"));

            _storageService.TryReadOrCreate(out _options, DefaultDirectoryNames.Configuration, ComponentHistoryServiceOptions.Filename);
            if (!_options.IsEnabled)
            {
                _logger.LogInformation("Component history is disabled.");
                return;
            }

            Task.Run(() => TryProcessWorkItems(_systemCancellationToken.Token), _systemCancellationToken.Token);
            Task.Run(() => TryUpdateComponentStatusValues(_systemCancellationToken.Token), _systemCancellationToken.Token);
        }

        public string BuildComponentHistoryPath(string componentUid)
        {
            return Path.Combine(_storageService.DataPath, "History", "Components", componentUid);
        }

        public string BuildComponentStatusHistoryPath(string componentUid, string statusUid)
        {
            return Path.Combine(BuildComponentHistoryPath(componentUid), "Status", statusUid);
        }

        public void Dispose()
        {
            _pendingStatusWorkItems.Dispose();
        }

        void OnComponentStatusChanged(object sender, ComponentStatusChangedEventArgs e)
        {
            var updateOnValueChanged = true;

            // TODO: Get filter settings and skip if disabled.

            if (!updateOnValueChanged)
            {
                _logger.LogTrace($"Skipping value changed update trigger for '{e.Component.Uid}.{e.StatusUid}'.");
                return;
            }

            TryEnqueueWorkItem(new ComponentStatusHistoryWorkItem
            {
                Timestamp = e.Timestamp,
                Component = e.Component,
                StatusUid = e.StatusUid,
                Value = e.NewValue
            });
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

                    foreach (var component in _componentRegistryService.GetComponents())
                    {
                        foreach (var status in component.GetStatus())
                        {
                            TryEnqueueWorkItem(new ComponentStatusHistoryWorkItem
                            {
                                Timestamp = DateTime.UtcNow,
                                Component = component,
                                StatusUid = status.Key,
                                Value = status.Value
                            });
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

        void TryEnqueueWorkItem(ComponentStatusHistoryWorkItem workItem)
        {
            try
            {
                if (IsComponentStatusBlacklisted(workItem.Component.Uid, workItem.StatusUid))
                {
                    return;
                }

                _pendingStatusWorkItems.Add(workItem);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while enqueue component status value '{workItem.Component.Uid}.{workItem.StatusUid}'.");
            }
        }

        bool IsComponentStatusBlacklisted(string componentUid, string statusUid)
        {
            if (_options.ComponentBlacklist?.Contains(componentUid) == true)
            {
                return true;
            }

            if (_options.StatusBlacklist?.Contains(statusUid) == true)
            {
                return true;
            }

            if (_options.ComponentStatusBlacklist?.Contains(componentUid + "." + statusUid) == true)
            {
                return true;
            }

            return false;
        }
    }
}
