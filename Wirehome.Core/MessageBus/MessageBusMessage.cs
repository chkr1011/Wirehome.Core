using System;
using System.Collections.Generic;

namespace Wirehome.Core.MessageBus
{
    public sealed class MessageBusMessage
    {
        public DateTime EnqueuedTimestamp { get; set; }

        public IDictionary<object, object> InnerMessage { get; set; } = new Dictionary<object, object>();
    }
}