using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wirehome.Core.Foundation.Model;
using Wirehome.Core.HTTP.Controllers.Models;
using Wirehome.Core.MessageBus;

namespace Wirehome.Core.HTTP.Controllers
{
    [ApiController]
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
        public Task<WirehomeDictionary> PostMessageWithReply(TimeSpan timeout, [FromBody] WirehomeDictionary messsage)
        {
            return _messageBusService.PublishRequestAsync(messsage, timeout);
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

                using (var timeoutToken = new CancellationTokenSource(TimeSpan.FromSeconds(timeout)))
                {
                    using (var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken.Token, HttpContext.RequestAborted))
                    {
                        using (linkedToken.Token.Register(() => { tcs.TrySetCanceled(); }))
                        {
                            return (await tcs.Task).Message;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return new WirehomeDictionary().WithType("exception.timeout");
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
        public IList<MessageBusMessage> GetHistory(string typeFilter = null)
        {
            var history = _messageBusService.GetHistory();

            if (!string.IsNullOrEmpty(typeFilter))
            {
                history.RemoveAll(i =>
                {
                    if (!i.Message.TryGetValue("type", out var typeValue))
                    {
                        return false;
                    }

                    var type = Convert.ToString(typeValue, CultureInfo.InvariantCulture);
                    return !string.Equals(type, typeFilter, StringComparison.Ordinal);
                });
            }

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
                FaultedMessagesCount = s.FaultedMessagesCount,
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
