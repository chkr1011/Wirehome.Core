using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wirehome.Core.App;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Extensions;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Resources;
using Wirehome.Core.Storage;
using Wirehome.Core.System;

namespace Wirehome.Core.Notifications;

/// <summary>
///     TODO: Expose publish method to function pool.
///     TODO: Expose publish method to message bus.
///     TODO: Add a dictionary of parameters to each notifications. They can be shown in the UI.
/// </summary>
public sealed class NotificationsService : WirehomeCoreService
{
    const string StorageFilename = "Notifications.json";
    readonly AppService _appService;
    readonly ILogger _logger;
    readonly MessageBusService _messageBusService;

    readonly List<Notification> _notifications = new();
    readonly ResourceService _resourcesService;

    readonly StorageService _storageService;
    readonly SystemCancellationToken _systemCancellationToken;

    public NotificationsService(StorageService storageService,
        SystemStatusService systemStatusService,
        ResourceService resourcesService,
        MessageBusService messageBusService,
        SystemCancellationToken systemCancellationToken,
        AppService appService,
        ILogger<NotificationsService> logger)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _resourcesService = resourcesService ?? throw new ArgumentNullException(nameof(resourcesService));
        _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
        _systemCancellationToken = systemCancellationToken ?? throw new ArgumentNullException(nameof(systemCancellationToken));
        _appService = appService ?? throw new ArgumentNullException(nameof(appService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (systemStatusService == null)
        {
            throw new ArgumentNullException(nameof(systemStatusService));
        }

        systemStatusService.Set("notifications.count", () =>
        {
            lock (_notifications)
            {
                return _notifications.Count;
            }
        });

        _appService.RegisterStatusProvider("notifications", GetNotifications);
    }

    public void Clear()
    {
        lock (_notifications)
        {
            _notifications.Clear();
            Save();

            _logger.Log(LogLevel.Information, "Removed all notifications.");
        }
    }

    public void DeleteNotification(Guid uid)
    {
        lock (_notifications)
        {
            _notifications.RemoveAll(n => n.Uid.Equals(uid));
            Save();

            _logger.Log(LogLevel.Information, $"Removed notification '{uid}'.");
        }
    }

    public List<Notification> GetNotifications()
    {
        lock (_notifications)
        {
            return new List<Notification>(_notifications);
        }
    }

    public void Publish(NotificationType type, string message, TimeSpan? timeToLive)
    {
        if (!timeToLive.HasValue)
        {
            if (!_storageService.SafeReadSerializedValue(out NotificationsServiceOptions options, DefaultDirectoryNames.Configuration, NotificationsServiceOptions.Filename))
            {
                options = new NotificationsServiceOptions();
            }

            if (type == NotificationType.Information)
            {
                timeToLive = options.DefaultTimeToLiveForInformation;
            }
            else if (type == NotificationType.Warning)
            {
                timeToLive = options.DefaultTimeToLiveForWarning;
            }
            else
            {
                timeToLive = options.DefaultTimeToLiveForError;
            }
        }

        var notification = new Notification
        {
            Uid = Guid.NewGuid(),
            Type = type,
            Message = message,
            Timestamp = DateTime.Now,
            TimeToLive = timeToLive.Value
        };

        Publish(notification);
    }

    public void PublishFromResource(PublishFromResourceParameters parameters)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        var message = _resourcesService.GetResourceValue(parameters.ResourceUid);
        message = _resourcesService.FormatValue(message, parameters.Parameters);

        Publish(parameters.Type, message, parameters.TimeToLive);
    }

    protected override void OnStart()
    {
        lock (_notifications)
        {
            Load();
        }

        ParallelTask.Start(() => RemoveNotificationsAsync(_systemCancellationToken.Token), _systemCancellationToken.Token, _logger);
    }

    void Load()
    {
        if (!_storageService.TryReadSerializedValue<List<Notification>>(out var notifications, "Notifications.json"))
        {
            return;
        }

        lock (_notifications)
        {
            foreach (var notification in notifications)
            {
                _notifications.Add(notification);
            }
        }
    }

    void Publish(Notification notification)
    {
        lock (_notifications)
        {
            _notifications.Add(notification);
            Save();
        }

        _messageBusService.Publish(new Dictionary<object, object>
        {
            ["type"] = "notifications.event.published",
            ["notification_type"] = notification.Type.ToString().ToLowerInvariant(),
            ["message"] = notification.Message,
            ["time_to_live"] = notification.TimeToLive.ToString("c")
        });
    }

    async Task RemoveNotificationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var saveIsRequired = false;

                lock (_notifications)
                {
                    for (var i = _notifications.Count - 1; i >= 0; i--)
                    {
                        var notification = _notifications[i];
                        if (notification.Timestamp.Add(notification.TimeToLive) >= now)
                        {
                            continue;
                        }

                        _notifications.RemoveAt(i);
                        saveIsRequired = true;
                    }

                    if (saveIsRequired)
                    {
                        Save();
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            _logger.Log(LogLevel.Error, exception, "Unhandled exception while removing notifications.");
        }
    }

    void Save()
    {
        _storageService.WriteSerializedValue(_notifications, StorageFilename);
    }
}