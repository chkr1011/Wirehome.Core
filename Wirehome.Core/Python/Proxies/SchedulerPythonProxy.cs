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

        public string start_thread(string uid, Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            if (string.IsNullOrEmpty(uid))
            {
                uid = Guid.NewGuid().ToString("N");
            }

            _schedulerService.StartThread(uid, _ => action());
            return uid;
        }

        public void stop_thread(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _schedulerService.StopThread();
        }

        public void start_timer(string uid, int interval, Action action)
        {
            _schedulerService.StartTimer(uid, TimeSpan.FromMilliseconds(interval), (s, span) =>  action());
        }

        public string start_countdown(string uid, long millies, Action<string> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            if (string.IsNullOrEmpty(uid))
            {
                uid = Guid.NewGuid().ToString("N");
            }

            _schedulerService.StartCountdown(uid, TimeSpan.FromMilliseconds(millies), callback);
            return uid;
        }

        public void stop_countdown(string uid)
        {
            _schedulerService.StopCountdown(uid);
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles