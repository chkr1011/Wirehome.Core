using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.System;

namespace Wirehome.Core.Scheduler
{
    public class SchedulerService : IService
    {
        readonly Dictionary<string, ActiveTimer> _activeTimers = new Dictionary<string, ActiveTimer>();
        readonly Dictionary<string, ActiveCountdown> _activeCountdowns = new Dictionary<string, ActiveCountdown>();
        readonly Dictionary<string, ActiveThread> _activeThreads = new Dictionary<string, ActiveThread>();

        readonly ILogger _logger;
        readonly SystemCancellationToken _systemCancellationToken;

        public SchedulerService(
            SystemStatusService systemStatusService,
            SystemCancellationToken systemCancellationToken,
            ILogger<SchedulerService> logger)
        {
            _systemCancellationToken = systemCancellationToken ?? throw new ArgumentNullException(nameof(systemCancellationToken));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (systemStatusService is null) throw new ArgumentNullException(nameof(systemStatusService));
            systemStatusService.Set("scheduler.active_threads", () => _activeThreads.Count);
            systemStatusService.Set("scheduler.active_timers", () => _activeTimers.Count);
            systemStatusService.Set("scheduler.active_countdowns", () => _activeCountdowns.Count);
        }

        public void Start()
        {
            var taskScheduler = new Thread(ScheduleTasks)
            {
                Name = nameof(SchedulerService),
                IsBackground = true
            };

            taskScheduler.Start();
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
                    using (existingTimer)
                    {
                        existingTimer.Stop();
                    }
                }

                _activeTimers[uid] = activeTimer;
            }

            activeTimer.Start(_systemCancellationToken.Token);

            return uid;
        }

        public bool StopTimer(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_activeTimers)
            {
                if (_activeTimers.TryGetValue(uid, out var activeTimer))
                {
                    using (activeTimer)
                    {
                        activeTimer.Stop();
                    }
                }

                return _activeTimers.Remove(uid);
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

        public bool StopCountdown(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_activeCountdowns)
            {
                return _activeCountdowns.Remove(uid);
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

            lock (_activeThreads)
            {
                StopThread(uid);

                var activeThread = new ActiveThread(uid, action, state, _logger, _systemCancellationToken.Token);
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

        public bool StopThread(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            try
            {
                lock (_activeThreads)
                {
                    if (_activeThreads.TryGetValue(uid, out var activeThread))
                    {
                        using (activeThread)
                        {
                            activeThread.Stop();
                        }

                        return true;
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while stopping thread '{uid}'.");
            }

            return false;
        }

        public IList<ActiveThread> GetActiveThreads()
        {
            lock (_activeThreads)
            {
                return _activeThreads.Values.ToList();
            }
        }

        public bool ThreadExists(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_activeThreads)
            {
                return _activeThreads.ContainsKey(uid);
            }
        }

        void ScheduleTasks()
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                while (!_systemCancellationToken.Token.IsCancellationRequested)
                {
                    var elapsed = stopwatch.Elapsed;
                    stopwatch.Restart();

                    UpdateActiveCountdowns(elapsed);

                    Thread.Sleep(10);
                }
            }
            catch (ThreadAbortException)
            {
            }
        }

        void UpdateActiveCountdowns(TimeSpan elapsed)
        {
            lock (_activeCountdowns)
            {
                var activeCountdowns = _activeCountdowns.ToList();

                foreach (var activeCountdown in activeCountdowns)
                {
                    activeCountdown.Value.TimeLeft -= elapsed;

                    if (activeCountdown.Value.TimeLeft > TimeSpan.Zero)
                    {
                        continue;
                    }

                    _activeCountdowns.Remove(activeCountdown.Key);
                    _logger.LogTrace($"Countdown '{activeCountdown.Key}' elapsed. Invoking callback.");

                    ThreadPool.QueueUserWorkItem(_ => activeCountdown.Value.TryInvokeCallback());
                }
            }
        }
    }
}
