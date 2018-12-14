#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using IronPython.Runtime;
using Wirehome.Core.Python;

namespace Wirehome.Core.Scheduler
{
    public class SchedulerServicePythonProxy : IInjectedPythonProxy
    {
        private readonly SchedulerService _schedulerService;

        public SchedulerServicePythonProxy(SchedulerService schedulerService)
        {
            _schedulerService = schedulerService ?? throw new ArgumentNullException(nameof(schedulerService));
        }

        public string ModuleName { get; } = "scheduler";

        public string start_thread(string uid, Action<PythonDictionary> callback, object state = null)
        {
            return _schedulerService.StartThread(uid, p => callback(PythonConvert.ToPythonDictionary(p)), state);
        }

        public bool stop_thread(string uid)
        {
            return _schedulerService.StopThread(uid);
        }

        public bool thread_exists(string uid)
        {
            return _schedulerService.ThreadExists(uid);
        }

        public string start_timer(string uid, int interval, Action<PythonDictionary> callback, object state = null)
        {
            return _schedulerService.StartTimer(uid, TimeSpan.FromMilliseconds(interval), p => callback(PythonConvert.ToPythonDictionary(p)), state);
        }

        public bool stop_timer(string uid)
        {
            return  _schedulerService.StopTimer(uid);
        }

        public bool timer_exists(string uid)
        {
            return _schedulerService.TimerExists(uid);
        }

        public string attach_to_default_timer(string uid, Action<PythonDictionary> callback, object state = null)
        {
            return _schedulerService.AttachToDefaultTimer(uid, p => callback(PythonConvert.ToPythonDictionary(p)), state);
        }

        public void detach_from_default_timer(string uid)
        {
            _schedulerService.DetachFromDefaultTimer(uid);
        }
        
        public string start_countdown(string uid, long duration, Action<PythonDictionary> callback, object state = null)
        {
            return _schedulerService.StartCountdown(uid, TimeSpan.FromMilliseconds(duration), p => callback(PythonConvert.ToPythonDictionary(p)), state);
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