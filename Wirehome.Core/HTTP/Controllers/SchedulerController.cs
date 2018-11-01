using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.HTTP.Controllers.Models;
using Wirehome.Core.Python.Models;
using Wirehome.Core.Scheduler;

namespace Wirehome.Core.HTTP.Controllers
{
    public class SchedulerController : Controller
    {
        private readonly SchedulerService _schedulerService;

        public SchedulerController(SchedulerService schedulerService)
        {
            _schedulerService = schedulerService ?? throw new ArgumentNullException(nameof(schedulerService));
        }

        [HttpGet]
        [Route("/api/v1/scheduler/active_threads")]
        [ApiExplorerSettings(GroupName = "v1")]
        public IDictionary<string, ActiveThreadModel> GetActiveThreads()
        {
            return _schedulerService.GetActiveThreads().ToDictionary(t => t.Uid, t => new ActiveThreadModel
            {
                CreatedTimestamp = t.CreatedTimestamp.ToString("O"),
                Uptime = (int)(DateTime.UtcNow - t.CreatedTimestamp).TotalMilliseconds,
                ManagedThreadId = t.ManagedThreadId
            });
        }

        [HttpDelete]
        [Route("/api/v1/scheduler/active_threads/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteActiveThread(string uid)
        {
            _schedulerService.StopThread(uid);
        }

        [HttpGet]
        [Route("/api/v1/scheduler/active_timers")]
        [ApiExplorerSettings(GroupName = "v1")]
        public IDictionary<string, ActiveTimerModel> GetActiveTimers()
        {
            return _schedulerService.GetActiveTimers().ToDictionary(t => t.Uid, t => new ActiveTimerModel
            {
                Interval = (int)t.Interval.TotalMilliseconds,
                LastException = new ExceptionPythonModel(t.LastException)
            });
        }

        [HttpDelete]
        [Route("/api/v1/scheduler/active_timers/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteActiveTimer(string uid)
        {
            _schedulerService.StopTimer(uid);
        }

        [HttpGet]
        [Route("/api/v1/scheduler/default_timer_subscribers")]
        [ApiExplorerSettings(GroupName = "v1")]
        public IList<string> GetDefaultTimerSubscribers()
        {
            return _schedulerService.GetDefaultTimerSubscribers().Select(s => s.Uid).ToList();
        }

        [HttpDelete]
        [Route("/api/v1/scheduler/default_timer_subscribers/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteDefaultTimerSubscriber(string uid)
        {
            _schedulerService.DetachFromDefaultTimer(uid);
        }

        [HttpGet]
        [Route("/api/v1/scheduler/active_countdowns")]
        [ApiExplorerSettings(GroupName = "v1")]
        public IDictionary<string, ActiveCountdownModel> GetActiveCountdowns()
        {
            return _schedulerService.GetActiveCountdowns().ToDictionary(t => t.Uid, t => new ActiveCountdownModel
            {
                TimeLeft = (int)t.TimeLeft.TotalMilliseconds
            });
        }

        [HttpDelete]
        [Route("/api/v1/scheduler/active_countdowns/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteActiveCountdown(string uid)
        {
            _schedulerService.StopCountdown(uid);
        }
    }
}
