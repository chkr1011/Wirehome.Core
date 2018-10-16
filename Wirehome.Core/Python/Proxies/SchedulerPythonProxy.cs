#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using IronPython.Runtime;
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

        public string start_thread(string uid, Action callback, object state = null)
        {
            // TODO: Migrate to callback with dictionary.
            return _schedulerService.StartThread(uid, _ => callback(), state);
        }

        public void stop_thread(string uid)
        {
            _schedulerService.StopThread(uid);
        }

        public string start_timer(string uid, int interval, Action<PythonDictionary> callback, object state = null)
        {
            return _schedulerService.StartTimer(uid, TimeSpan.FromMilliseconds(interval), p => callback(PythonConvert.ToPythonDictionary(p)), state);
        }

        public void stop_timer(string uid)
        {
            _schedulerService.StopTimer(uid);
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

        public void stop_countdown(string uid)
        {
            _schedulerService.StopCountdown(uid);
        }
    }
}