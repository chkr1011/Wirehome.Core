using System;
using System.Collections.Generic;

namespace Wirehome.Core.MessageBus;

public sealed class MessageBusMessage
{
    public MessageBusMessage(IDictionary<object, object> innerMessage)
    {
        InnerMessage = innerMessage ?? throw new ArgumentNullException(nameof(innerMessage));
    }

    public DateTime EnqueuedTimestamp { get; set; }

    public IDictionary<object, object> InnerMessage { get; }
}