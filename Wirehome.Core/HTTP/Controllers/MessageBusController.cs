using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wirehome.Core.HTTP.Controllers.Models;
using Wirehome.Core.MessageBus;

namespace Wirehome.Core.HTTP.Controllers
{
    [ApiController]
    public class MessageBusController : Controller
    {
        readonly MessageBusService _messageBusService;

        public MessageBusController(MessageBusService messageBusService)
        {
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
        }

        [HttpPost]
        [Route("/api/v1/message_bus/message")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostMessage([FromBody] IDictionary<object, object> messsage)
        {
            _messageBusService.Publish(messsage);
        }

        [HttpPost]
        [Route("/api/v1/message_bus/wait_for")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task<IDictionary<object, object>> PostWaitForAsync([FromBody] IEnumerable<IDictionary<object, object>> filters, int timeout = 60)
        {
            if (filters == null) throw new ArgumentNullException(nameof(filters));

            var subscriptions = new List<string>();
            try
            {
                var tcs = new TaskCompletionSource<IDictionary<object, object>>();

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
                            return await tcs.Task.ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return new Dictionary<object, object>
                {
                    ["type"] = "exception.timeout"
                };
            }
            finally
            {
                foreach (var subscription in subscriptions)
                {
                    _messageBusService.Unsubscribe(subscription);
                }
            }
        }

        [HttpPost]
        [Route("/api/v1/message_bus/history/enable")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void EnableHistory(int maxMessagesCount = 100)
        {
            _messageBusService.EnableHistory(maxMessagesCount);
        }

        [HttpPost]
        [Route("/api/v1/message_bus/history/disable")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DisableHistory()
        {
            _messageBusService.DisableHistory();
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
