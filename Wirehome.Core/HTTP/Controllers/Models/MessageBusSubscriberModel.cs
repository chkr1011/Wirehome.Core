using System.Collections.Generic;

namespace Wirehome.Core.HTTP.Controllers.Models
{
    public class MessageBusSubscriberModel
    {
        public IDictionary<object, object> Filter { get; set; }

        public int ProcessedMessagesCount { get; set; }

        public int PendingMessagesCount { get; set; }

        public int FaultedMessagesCount { get; set; }
    }
}
