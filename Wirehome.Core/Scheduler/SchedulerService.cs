using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Storage;
using Wirehome.Core.System;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Wirehome.Core.Scheduler
{
    public class SchedulerService : IService
    {
        private readonly Dictionary<string, ActiveTimer> _activeTimers = new Dictionary<string, ActiveTimer>();
        private readonly Dictionary<string, ActiveCountdown> _activeCountdowns = new Dictionary<string, ActiveCountdown>();
        private readonly Dictionary<string, ActiveThread> _activeThreads = new Dictionary<string, ActiveThread>();
        private readonly Dictionary<string, DefaultTimerSubscriber> _defaultTimerSubscribers = new Dictionary<string, DefaultTimerSubscriber>();

        private readonly ILogger _logger;
        //private readonly IScheduler _scheduler;
        private readonly SystemCancellationToken _systemCancellationToken;
        private readonly StorageService _storageService;

        public SchedulerService(
            SystemStatusService systemStatusService,
            SystemCancellationToken systemCancellationToken,
            StorageService storageService,
            ILogger<SchedulerService> logger)
        {
            _systemCancellationToken = systemCancellationToken ?? throw new ArgumentNullException(nameof(systemCancellationToken));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            systemStatusService.Set("scheduler.active_threads", () => _activeThreads.Count);
            systemStatusService.Set("scheduler.active_timers", () => _activeTimers.Count);
            systemStatusService.Set("scheduler.active_countdowns", () => _activeCountdowns.Count);
        }

        public void Start()
        {
            Task.Factory.StartNew(ScheduleTasks, _systemCancellationToken.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            //LogProvider.SetCurrentLogProvider(new QuartzLogBridge(_logger));

            //GlobalConfiguration.Configuration.UseStorage()
            //_backgroundJobServer = new BackgroundJobServer();
        }

        public string AttachToDefaultTimer(string uid, Action<TimerTickCallbackParameters> callback, object state = null)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            if (string.IsNullOrEmpty(uid))
            {
                uid = Guid.NewGuid().ToString("D");
            }

            lock (_defaultTimerSubscribers)
            {
                _defaultTimerSubscribers[uid] = new DefaultTimerSubscriber(uid, callback, state, _logger);
            }

            return uid;
        }

        public void DetachFromDefaultTimer(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_defaultTimerSubscribers)
            {
                _defaultTimerSubscribers.Remove(uid);
            }
        }

        public List<DefaultTimerSubscriber> GetDefaultTimerSubscribers()
        {
            lock (_defaultTimerSubscribers)
            {
                return _defaultTimerSubscribers.Values.ToList();
            }
        }

        public string StartTimer(string uid, TimeSpan interval, Action<TimerTickCallbackParameters> callback, object state = null)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            if (string.IsNullOrEmpty(uid))
            {
                uid = Guid.NewGuid().ToString("D");
            }

            var activeTimer = new ActiveTimer(uid, interval, callback, state, _logger);
            lock (_activeTimers)
            {
                if (_activeTimers.TryGetValue(uid, out var existingTimer))
                {
                    existingTimer.Stop();
                    existingTimer.Dispose();
                }

                _activeTimers[uid] = activeTimer;
            }

            activeTimer.Start();

            return uid;
        }

        public void StopTimer(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_activeTimers)
            {
                if (_activeTimers.TryGetValue(uid, out var activeTimer))
                {
                    activeTimer.Stop();
                    activeTimer.Dispose();
                }

                _activeTimers.Remove(uid);
            }
        }

        public List<ActiveTimer> GetActiveTimers()
        {
            lock (_activeThreads)
            {
                return _activeTimers.Values.ToList();
            }
        }

        public bool TimerExists(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_activeTimers)
            {
                return _activeTimers.ContainsKey(uid);
            }
        }

        public string StartCountdown(string uid, TimeSpan timeLeft, Action<CountdownElapsedParameters> callback, object state = null)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            if (string.IsNullOrEmpty(uid))
            {
                uid = Guid.NewGuid().ToString("D");
            }

            lock (_activeCountdowns)
            {
                _activeCountdowns[uid] = new ActiveCountdown(uid, callback, state, _logger)
                {
                    TimeLeft = timeLeft
                };
            }

            return uid;
        }

        public void StopCountdown(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_activeCountdowns)
            {
                _activeCountdowns.Remove(uid);
            }
        }

        public List<ActiveCountdown> GetActiveCountdowns()
        {
            lock (_activeCountdowns)
            {
                return _activeCountdowns.Values.ToList();
            }
        }

        public bool CountdownExists(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_activeCountdowns)
            {
                return _activeCountdowns.ContainsKey(uid);
            }
        }
        
        public string StartThread(string uid, Action<StartThreadCallbackParameters> action, object state = null)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            if (string.IsNullOrEmpty(uid))
            {
                uid = Guid.NewGuid().ToString("D");
            }

            _logger.Log(LogLevel.Debug, $"Starting new thread '{uid}'.");

            lock (_activeThreads)
            {
                StopThread(uid);

                var activeThread = new ActiveThread(uid, action, state, _systemCancellationToken.Token, _logger);
                activeThread.StoppedCallback = () =>
                {
                    lock (_activeThreads)
                    {
                        _activeThreads.Remove(uid);
                        activeThread.Dispose();
                    }
                };

                _activeThreads[uid] = activeThread;
                activeThread.Start();
            }

            return uid;
        }

        public void StopThread(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            try
            {
                lock (_activeThreads)
                {
                    if (_activeThreads.TryGetValue(uid, out var activeThread))
                    {
                        activeThread.Stop();
                        activeThread.Dispose();
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, exception, $"Error while stopping thread '{uid}'.");
            }
        }

        public IList<ActiveThread> GetActiveThreads()
        {
            lock (_activeThreads)
            {
                return _activeThreads.Values.ToList();
            }
        }

        private void ScheduleTasks()
        {
            Thread.CurrentThread.Name = nameof(ScheduleTasks);

            var stopwatch = Stopwatch.StartNew();

            while (!_systemCancellationToken.Token.IsCancellationRequested)
            {
                var elapsed = stopwatch.Elapsed;
                stopwatch.Restart();

                UpdateActiveCountdowns(elapsed);
                InvokeDefaultTimerSubscribers();

                Thread.Sleep(10);
            }
        }

        private void InvokeDefaultTimerSubscribers()
        {
            lock (_defaultTimerSubscribers)
            {
                foreach (var defaultTimerSubscriber in _defaultTimerSubscribers)
                {
                    defaultTimerSubscriber.Value.TryInvokeCallback();
                }
            }
        }

        private void UpdateActiveCountdowns(TimeSpan elapsed)
        {
            lock (_activeCountdowns)
            {
                foreach (var key in _activeCountdowns.Keys.ToList())
                {
                    var activeCountdown = _activeCountdowns[key];

                    activeCountdown.TimeLeft -= elapsed;

                    if (activeCountdown.TimeLeft <= TimeSpan.Zero)
                    {
                        _activeCountdowns.Remove(key);

                        Task.Run(() =>
                        {
                            activeCountdown.TryInvokeCallback();
                        });
                    }
                }
            }
        }
    }
}
