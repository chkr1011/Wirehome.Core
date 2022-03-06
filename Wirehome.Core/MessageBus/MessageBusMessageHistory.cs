using System;
using System.Collections.Generic;

namespace Wirehome.Core.MessageBus;

public sealed class MessageBusMessageHistory
{
    readonly LinkedList<MessageBusMessage> _messages = new();

    bool _isEnabled;
    int _maxMessagesCount;

    public void Add(MessageBusMessage message)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

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

            // The latest message should be the first in the list!
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

    public void Disable()
    {
        _isEnabled = false;
    }

    public void Enable(int maxMessagesCount)
    {
        _maxMessagesCount = maxMessagesCount;
        _isEnabled = true;
    }

    public List<MessageBusMessage> GetMessages()
    {
        lock (_messages)
        {
            return new List<MessageBusMessage>(_messages);
        }
    }
}