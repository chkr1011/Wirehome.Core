using System;
using System.Collections.Generic;

namespace Wirehome.Core.MessageBus
{
    public class MessageBusMessage
    {
        public Guid Uid { get; } = Guid.NewGuid();

        public DateTime EnqueuedTimestamp { get; set; }

        public IDictionary<object, object> Message { get; set; } = new Dictionary<object, object>();
    }
}