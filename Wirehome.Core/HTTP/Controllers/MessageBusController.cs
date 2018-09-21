using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Model;

namespace Wirehome.Core.HTTP.Controllers
{
    public class MessageBusController : Controller
    {
        private readonly MessageBusService _messageBusService;
        
        public MessageBusController(MessageBusService messageBusService)
        {
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
        }

        [HttpPost]
        [Route("/api/v1/message_bus/message")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostMessage([FromBody] WirehomeDictionary messsage)
        {
            if (messsage == null) throw new ArgumentNullException(nameof(messsage));

            _messageBusService.Publish(messsage);
        }
        
        [HttpPost]
        [Route("/api/v1/message_bus/wait_for")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task<WirehomeDictionary> PostWaitForAsync([FromBody] IEnumerable<WirehomeDictionary> filters, int timeout = 60)
        {
            if (filters == null) throw new ArgumentNullException(nameof(filters));

            var subscriptions = new List<string>();
            try
            {
                var tcs = new TaskCompletionSource<WirehomeDictionary>();
                foreach (var filter in filters)
                {
                    subscriptions.Add(_messageBusService.Subscribe(filter, m => tcs.TrySetResult(m)));
                }

                var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                var cts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, HttpContext.RequestAborted);
                var timeoutTask = Task.Run(async () => await Task.Delay(TimeSpan.FromSeconds(timeout), cts.Token), cts.Token);
                var finishedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (finishedTask == timeoutTask)
                {
                    return new WirehomeDictionary().WithType("exception.timeout");
                }

                return tcs.Task.Result;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            finally
            {
                foreach (var subscription in subscriptions)
                {
                    _messageBusService.Unsubscribe(subscription);
                }
            }
        }

        [HttpGet]
        [Route("/api/v1/message_bus/history")]
        [ApiExplorerSettings(GroupName = "v1")]
        public List<BusMessage> GetHistory()
        {
            var history = _messageBusService.GetHistory();
            history.Reverse();
            return history;
        }
    }
}
