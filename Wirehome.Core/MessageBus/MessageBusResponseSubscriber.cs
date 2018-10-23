using System.Threading.Tasks;

namespace Wirehome.Core.MessageBus
{
    public class MessageBusResponseSubscriber
    {
        private readonly TaskCompletionSource<MessageBusMessage> _taskCompletionSource = new TaskCompletionSource<MessageBusMessage>();

        public Task<MessageBusMessage> Task => _taskCompletionSource.Task;

        public void SetResponse(MessageBusMessage responseMessage)
        {
            _taskCompletionSource.TrySetResult(responseMessage);
        }
    }
}
