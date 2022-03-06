using System.Collections.Generic;

namespace Wirehome.Core.HTTP.Controllers.Models
{
    public sealed class MessageBusSubscriberModel
    {
        public IDictionary<string, string> Filter { get; set; }

        public long ProcessedMessagesCount { get; set; }

        public long FaultedMessagesCount { get; set; }
    }
}
