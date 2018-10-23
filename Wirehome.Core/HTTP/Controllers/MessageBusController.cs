using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.HTTP.Controllers.Models;
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
            _messageBusService.Publish(messsage);
        }

        [HttpPost]
        [Route("/api/v1/message_bus/message_with_reply")]
        [ApiExplorerSettings(GroupName = "v1")]
        public WirehomeDictionary PostMessageWithReply([FromBody] WirehomeDictionary messsage)
        {
            throw new NotImplementedException();
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
                var tcs = new TaskCompletionSource<MessageBusMessage>();
                foreach (var filter in filters)
                {
                    var subscriptionUid = "api_wait_for:" + Guid.NewGuid().ToString("D");
                    subscriptions.Add(_messageBusService.Subscribe(subscriptionUid, filter, m => tcs.TrySetResult(m)));
                }

                var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                var cts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, HttpContext.RequestAborted);
                var timeoutTask = Task.Run(async () => await Task.Delay(TimeSpan.FromSeconds(timeout), cts.Token), cts.Token);
                var finishedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (finishedTask == timeoutTask)
                {
                    return new WirehomeDictionary().WithType("exception.timeout");
                }

                return tcs.Task.Result.Message;
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
        public IList<MessageBusMessage> GetHistory()
        {
            var history = _messageBusService.GetHistory();
            history.Reverse();
            return history;
        }

        [HttpDelete]
        [Route("/api/v1/message_bus/history")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteHistory()
        {
            _messageBusService.ClearHistory();
        }

        [HttpGet]
        [Route("/api/v1/message_bus/subscribers")]
        [ApiExplorerSettings(GroupName = "v1")]
        public IDictionary<string, MessageBusSubscriberModel> GetSubscribers()
        {
            return _messageBusService.GetSubscribers().ToDictionary(s => s.Uid, s => new MessageBusSubscriberModel
            {
                ProcessedMessagesCount = s.ProcessedMessagesCount,
                PendingMessagesCount = s.PendingMessagesCount,
                Filter = s.Filter
            });
        }

        [HttpDelete]
        [Route("/api/v1/message_bus/subscribers/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteSubscriber(string uid)
        {
            _messageBusService.Unsubscribe(uid);
        }
    }
}
