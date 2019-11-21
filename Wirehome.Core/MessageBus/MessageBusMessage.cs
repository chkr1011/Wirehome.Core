using System;
using Wirehome.Core.Foundation.Model;

namespace Wirehome.Core.MessageBus
{
    public class MessageBusMessage
    {
        public Guid Uid { get; } = Guid.NewGuid();

        public DateTime EnqueuedTimestamp { get; set; }

        public WirehomeDictionary Message { get; set; } = new WirehomeDictionary();
    }
}