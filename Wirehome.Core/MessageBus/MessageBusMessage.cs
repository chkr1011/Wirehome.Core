using System;
using Wirehome.Core.Model;

namespace Wirehome.Core.MessageBus
{
    public class MessageBusMessage
    {
        public Guid Uid { get; } = Guid.NewGuid();

        public Guid? ResponseUid { get; set; }

        public DateTime EnqueuedTimestamp { get; set; }

        public WirehomeDictionary Message { get; set; } = new WirehomeDictionary();
    }
}