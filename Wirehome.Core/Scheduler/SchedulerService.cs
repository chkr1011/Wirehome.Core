using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Proxies;
using Wirehome.Core.System;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Wirehome.Core.Scheduler
{
    public class SchedulerService
    {
        private readonly ILogger _logger;
        private readonly IScheduler _scheduler;
        private readonly SystemService _systemService;
        private readonly ILoggerFactory _loggerFactory;

        private readonly Dictionary<string, ActiveTimer> _activeTimers = new Dictionary<string, ActiveTimer>();
        private readonly Dictionary<string, ActiveCountdown> _activeCountdowns = new Dictionary<string, ActiveCountdown>();
        private readonly Dictionary<string, ActiveThread> _activeThreads = new Dictionary<string, ActiveThread>();

        public SchedulerService(
            PythonEngineService pythonEngineService,
            SystemStatusService systemStatusService,
            SystemService systemService,
            ILoggerFactory loggerFactory)
        {
            _systemService = systemService ?? throw new ArgumentNullException(nameof(systemService));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

            _logger = loggerFactory.CreateLogger<SchedulerService>();

            LogProvider.SetCurrentLogProvider(new QuartzLogBridge(_logger));

            var configuration = new NameValueCollection
            {
                { "quartz.serializer.type", "binary" }
            };

            var factory = new StdSchedulerFactory(configuration);
            _scheduler = factory.GetScheduler().GetAwaiter().GetResult();

            if (pythonEngineService == null) throw new ArgumentNullException(nameof(pythonEngineService));
            pythonEngineService.RegisterSingletonProxy(new SchedulerPythonProxy(this));

            systemStatusService.Set("scheduler.active_threads", () => _activeThreads.Count);
            systemStatusService.Set("scheduler.active_timers", () => _activeTimers.Count);
            systemStatusService.Set("scheduler.active_countdowns", () => _activeCountdowns.Count);
        }

        public void Start()
        {
            _scheduler.Start().GetAwaiter().GetResult();

            Task.Factory.StartNew(ScheduleTasks, _systemService.CancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            StartTimer("default", TimeSpan.FromMilliseconds(100), DefaultTimerCallback);
        }

        //public string AttachToDefaultTimer()
        //{

        //}

        public void StartTimer(string uid, TimeSpan interval, Action<string, TimeSpan> callback)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            var activeTimer = new ActiveTimer(uid, interval, callback, _loggerFactory);
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

        public IList<ActiveTimer> GetActiveTimers()
        {
            lock (_activeThreads)
            {
                return _activeTimers.Values.ToList();
            }
        }

        public void StartCountdown(string uid, TimeSpan timeLeft, Action callback)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            lock (_activeCountdowns)
            {
                if (_activeCountdowns.TryGetValue(uid, out var activeCountdown))
                {
                    activeCountdown.TimeLeft = timeLeft;
                }
                else
                {
                    _activeCountdowns.Add(uid, new ActiveCountdown(uid, callback, _loggerFactory)
                    {
                        TimeLeft = timeLeft
                    });
                }
            }
        }

        public void StopCountdown(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_activeCountdowns)
            {
                _activeCountdowns.Remove(uid);
            }
        }

        public IList<ActiveCountdown> GetActiveCountdowns()
        {
            lock (_activeCountdowns)
            {
                return _activeCountdowns.Values.ToList();
            }
        }

        public void StartThread(string uid, Action<string> action)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (action == null) throw new ArgumentNullException(nameof(action));

            _logger.Log(LogLevel.Debug, $"Starting new thread '{uid}'.");

            lock (_activeThreads)
            {
                StopThread(uid);

                var activeThread = new ActiveThread(uid, action, _systemService.CancellationToken, _logger);
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
            var stopwatch = Stopwatch.StartNew();

            while (!_systemService.CancellationToken.IsCancellationRequested)
            {
                var elapsed = stopwatch.Elapsed;
                stopwatch.Restart();

                var finishedCountdowns = new List<ActiveCountdown>();

                lock (_activeCountdowns)
                {
                    var keys = _activeCountdowns.Keys.ToList();
                    foreach (var key in keys)
                    {
                        var activeCountdown = _activeCountdowns[key];

                        activeCountdown.TimeLeft -= elapsed;

                        if (activeCountdown.TimeLeft <= TimeSpan.Zero)
                        {
                            finishedCountdowns.Add(activeCountdown);
                            _activeCountdowns.Remove(key);
                        }
                    }
                }

                foreach (var finishedCountdown in finishedCountdowns)
                {
                    Task.Run(() =>
                    {
                        finishedCountdown.TryInvokeCallback();
                    });
                }

                Thread.Sleep(100);
            }
        }

        private void DefaultTimerCallback(string arg1, TimeSpan arg2)
        {

        }
    }
}
