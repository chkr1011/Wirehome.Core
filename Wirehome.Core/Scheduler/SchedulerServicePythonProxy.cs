#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using IronPython.Runtime;
using System;
using Wirehome.Core.Python;

namespace Wirehome.Core.Scheduler
{
    public sealed class SchedulerServicePythonProxy : IInjectedPythonProxy
    {
        readonly SchedulerService _schedulerService;

        public SchedulerServicePythonProxy(SchedulerService schedulerService)
        {
            _schedulerService = schedulerService ?? throw new ArgumentNullException(nameof(schedulerService));
        }

        public string ModuleName { get; } = "scheduler";

        public delegate void TimerCallback(PythonDictionary eventArgs);

        public delegate void CountdownCallback(PythonDictionary eventArgs);

        public delegate void ThreadCallback(PythonDictionary eventArgs);

        public string start_thread(string uid, ThreadCallback callback, object state = null)
        {
            return _schedulerService.StartThread(uid, p =>
            {
                var pythonDictionary = new PythonDictionary
                {
                    ["thread_uid"] = uid,
                    ["state"] = p.State
                };

                callback(pythonDictionary);
            }, state);
        }

        public bool stop_thread(string uid)
        {
            return _schedulerService.StopThread(uid);
        }

        public bool thread_exists(string uid)
        {
            return _schedulerService.ThreadExists(uid);
        }

        public string start_timer(string uid, int interval, TimerCallback callback, object state = null)
        {
            return _schedulerService.StartTimer(uid, TimeSpan.FromMilliseconds(interval), p =>
            {
                var pythonDictionary = new PythonDictionary
                {
                    ["timer_uid"] = uid,
                    ["elapsed_millis"] = p.ElapsedMillis,
                    ["state"] = p.State
                };

                callback(pythonDictionary);
            }, state);
        }

        public bool stop_timer(string uid)
        {
            return _schedulerService.StopTimer(uid);
        }

        public bool timer_exists(string uid)
        {
            return _schedulerService.TimerExists(uid);
        }

        public string start_countdown(string uid, long duration, CountdownCallback callback, object state = null)
        {
            return _schedulerService.StartCountdown(uid, TimeSpan.FromMilliseconds(duration), p =>
            {
                var pythonDictionary = new PythonDictionary
                {
                    ["countdown_uid"] = uid,
                    ["state"] = p.State
                };

                callback(pythonDictionary);
            }, state);
        }

        public bool stop_countdown(string uid)
        {
            return _schedulerService.StopCountdown(uid);
        }

        public bool countdown_exists(string uid)
        {
            return _schedulerService.CountdownExists(uid);
        }
    }
}