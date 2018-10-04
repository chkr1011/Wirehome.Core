#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using Wirehome.Core.Scheduler;

namespace Wirehome.Core.Python.Proxies
{
    public class SchedulerPythonProxy : IPythonProxy
    {
        private readonly SchedulerService _schedulerService;

        public SchedulerPythonProxy(SchedulerService schedulerService)
        {
            _schedulerService = schedulerService ?? throw new ArgumentNullException(nameof(schedulerService));
        }

        public string ModuleName { get; } = "scheduler";

        public string start_thread(string uid, Action callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            if (string.IsNullOrEmpty(uid))
            {
                uid = Guid.NewGuid().ToString("D");
            }

            _schedulerService.StartThread(uid, _ => callback());
            return uid;
        }

        public void stop_thread(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _schedulerService.StopThread(uid);
        }

        public string start_timer(string uid, int interval, Action<object> callback, object state = null)
        {
            if (string.IsNullOrEmpty(uid))
            {
                uid = Guid.NewGuid().ToString("D");
            }

            _schedulerService.StartTimer(uid, TimeSpan.FromMilliseconds(interval), (s, span) => callback(state));
            return uid;
        }

        public void stop_timer(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _schedulerService.StopTimer(uid);
        }

        public string start_countdown(string uid, long duration, Action<object> callback, object state = null)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            if (string.IsNullOrEmpty(uid))
            {
                uid = Guid.NewGuid().ToString("D");
            }

            _schedulerService.StartCountdown(uid, TimeSpan.FromMilliseconds(duration), () =>  callback(state));
            return uid;
        }

        public void stop_countdown(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _schedulerService.StopCountdown(uid);
        }
    }
}