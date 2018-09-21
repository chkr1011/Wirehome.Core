using System;
using Wirehome.Core.Model;

namespace Wirehome.Core.MessageBus
{
    public class BusMessage
    {
        public Guid Uid { get; set; }

        public DateTime? EnqueuedTimestamp { get; set; }

        public WirehomeDictionary Properties { get; set; }
    }
}