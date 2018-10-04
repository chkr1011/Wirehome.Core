using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
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
        [Route("/api/scheduler/active_threads")]
        [ApiExplorerSettings(GroupName = "v1")]
        public IList<string> GetActiveThreads()
        {
            return _schedulerService.GetActiveThreads();
        }

        [HttpDelete]
        [Route("/api/scheduler/active_threads/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteActiveThread(string uid)
        {
            _schedulerService.StopThread(uid);
        }

        [HttpGet]
        [Route("/api/scheduler/active_timers")]
        [ApiExplorerSettings(GroupName = "v1")]
        public IList<string> GetActiveTimers()
        {
            return _schedulerService.GetActiveTimers();
        }

        [HttpDelete]
        [Route("/api/scheduler/active_timers/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteActiveTimer(string uid)
        {
            _schedulerService.StopTimer(uid);
        }

        [HttpGet]
        [Route("/api/scheduler/active_countdowns")]
        [ApiExplorerSettings(GroupName = "v1")]
        public IList<string> GetActiveCountdowns()
        {
            return _schedulerService.GetActiveCountdowns();
        }

        [HttpDelete]
        [Route("/api/scheduler/active_countdowns/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteActiveCountdown(string uid)
        {
            _schedulerService.StopCountdown(uid);
        }
    }
}
