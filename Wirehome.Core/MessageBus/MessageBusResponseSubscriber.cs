using System;
using System.Threading.Tasks;

namespace Wirehome.Core.MessageBus
{
    public class MessageBusResponseSubscriber
    {
        readonly TaskCompletionSource<MessageBusMessage> _taskCompletionSource = new TaskCompletionSource<MessageBusMessage>();

        public Task<MessageBusMessage> Task => _taskCompletionSource.Task;

        public void SetResponse(MessageBusMessage responseMessage)
        {
            if (responseMessage == null) throw new ArgumentNullException(nameof(responseMessage));

            _taskCompletionSource.TrySetResult(responseMessage);
        }
    }
}
