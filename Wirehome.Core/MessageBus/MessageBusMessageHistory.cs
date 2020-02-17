using System.Collections.Generic;

namespace Wirehome.Core.MessageBus
{
    public class MessageBusMessageHistory
    {
        readonly LinkedList<MessageBusMessage> _messages = new LinkedList<MessageBusMessage>();

        bool _isEnabled;
        int _maxMessagesCount;

        public void Enable(int maxMessagesCount)
        {
            _maxMessagesCount = maxMessagesCount;
            _isEnabled = true;
        }

        public void Disable()
        {
            _isEnabled = false;
        }

        public void Add(MessageBusMessage message)
        {
            if (message is null) throw new global::System.ArgumentNullException(nameof(message));

            if (!_isEnabled)
            {
                return;
            }

            lock (_messages)
            {
                while (_messages.Count >= _maxMessagesCount)
                {
                    _messages.RemoveLast();
                }

                // TODO: Create copy!
                _messages.AddFirst(message);
            }
        }

        public void Clear()
        {
            lock (_messages)
            {
                _messages.Clear();
            }
        }

        public List<MessageBusMessage> GetMessages()
        {
            lock (_messages)
            {
                return new List<MessageBusMessage>(_messages);
            }
        }
    }
}
