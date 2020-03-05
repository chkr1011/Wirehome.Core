using System.Collections.Generic;

namespace Wirehome.Core.HTTP.Controllers.Models
{
    public class MessageBusSubscriberModel
    {
        public IDictionary<object, object> Filter { get; set; }

        public long ProcessedMessagesCount { get; set; }

        public long PendingMessagesCount { get; set; }

        public long FaultedMessagesCount { get; set; }
    }
}
