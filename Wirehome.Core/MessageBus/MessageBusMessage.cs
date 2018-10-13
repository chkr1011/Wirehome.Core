using System;
using Wirehome.Core.Model;

namespace Wirehome.Core.MessageBus
{
    public class MessageBusMessage
    {
        public Guid Uid { get; set; }

        public DateTime? EnqueuedTimestamp { get; set; }

        public WirehomeDictionary CarriedMessage { get; set; }
    }
}