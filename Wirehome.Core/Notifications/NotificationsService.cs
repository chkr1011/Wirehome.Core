using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Model;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Proxies;
using Wirehome.Core.Resources;
using Wirehome.Core.Storage;
using Wirehome.Core.System;

namespace Wirehome.Core.Notifications
{
    /// <summary>
    /// TODO: Expose publish method to function pool.
    /// TODO: Expose publich method to message bus.
    /// </summary>
    public class NotificationsService
    {
        private const string StorageFilename = "Notifications.json";

        private readonly List<Notification> _notifications = new List<Notification>();

        private readonly StorageService _storageService;
        private readonly ResourcesService _resourcesService;
        private readonly MessageBusService _messageBusService;
        private readonly SystemService _systemService;

        private readonly ILogger<NotificationsService> _logger;

        public NotificationsService(
            StorageService storageService,
            SystemStatusService systemStatusService,
            ResourcesService resourcesService,
            PythonEngineService pythonEngineService,
            MessageBusService messageBusService,
            SystemService systemService,
            ILoggerFactory loggerFactory)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _resourcesService = resourcesService ?? throw new ArgumentNullException(nameof(resourcesService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _systemService = systemService ?? throw new ArgumentNullException(nameof(systemService));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<NotificationsService>();

            pythonEngineService.RegisterSingletonProxy(new NotificationsPythonProxy(this));

            if (systemStatusService == null) throw new ArgumentNullException(nameof(systemStatusService));
            systemStatusService.Set("notifications.count", () =>
            {
                lock (_notifications)
                {
                    return _notifications.Count;
                }
            });
        }

        public void Start()
        {
            lock (_notifications)
            {
                Load();
            }

            Task.Run(() => RemoveNotificationsAsync(_systemService.CancellationToken), _systemService.CancellationToken);
        }

        public List<Notification> GetNotifications()
        {
            lock (_notifications)
            {
                return new List<Notification>(_notifications);
            }
        }

        public void PublishFromResource(PublishFromResourceParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            var message = _resourcesService.ResolveString(parameters.ResourceUid);
            message = _resourcesService.FormatValue(message, parameters.Parameters);

            Publish(parameters.Type, message, parameters.TimeToLive);
        }

        public void Publish(NotificationType type, string message, TimeSpan? timeToLive)
        {
            if (!timeToLive.HasValue)
            {
                if (!_storageService.TryRead(out NotificationsServiceSettings settings, "NotificationService.json"))
                {
                    settings = new NotificationsServiceSettings();
                }

                if (type == NotificationType.Information)
                {
                    timeToLive = settings.DefaultTimeToLiveForInformation;
                }
                else if (type == NotificationType.Warning)
                {
                    timeToLive = settings.DefaultTimeToLiveForWarning;
                }
                else
                {
                    timeToLive = settings.DefaultTimeToLiveForError;
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

        public void DeleteNotification(Guid uid)
        {
            lock (_notifications)
            {
                _notifications.RemoveAll(n => n.Uid.Equals(uid));
                Save();

                _logger.Log(LogLevel.Information, $"Removed notification '{uid}'.");
            }
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

        private void Publish(Notification notification)
        {
            lock (_notifications)
            {
                _notifications.Add(notification);
                Save();
            }

            _messageBusService.Publish(new WirehomeDictionary()
                .WithType("notifications.event.published")
                .WithValue("notification_type", notification.Type.ToString().ToLowerInvariant())
                .WithValue("message", notification.Message)
                .WithValue("time_to_live", notification.TimeToLive.ToString("c")));
        }

        private async Task RemoveNotificationsAsync(CancellationToken cancellationToken)
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

        private void Load()
        {
            if (!_storageService.TryRead<List<Notification>>(out var notifications, "Notifications.json"))
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

        private void Save()
        {
            _storageService.Write(_notifications, StorageFilename);
        }
    }
}
