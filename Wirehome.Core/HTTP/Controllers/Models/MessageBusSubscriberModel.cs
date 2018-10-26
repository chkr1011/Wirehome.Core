using Wirehome.Core.Model;

namespace Wirehome.Core.HTTP.Controllers.Models
{
    public class MessageBusSubscriberModel
    {
        public WirehomeDictionary Filter { get; set; }

        public int ProcessedMessagesCount { get; set; }

        public int PendingMessagesCount { get; set; }
    }
}
