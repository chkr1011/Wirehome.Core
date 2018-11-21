using System;

namespace Wirehome.Core.MessageBus
{
    public class MessageBusServiceOptions
    {
        public const string Filename = "MessageBusServiceConfiguration.json";

        public int HistoryItemsCount { get; set; } = 100;

        public int MessageProcessorsCount { get; set; } = Environment.ProcessorCount;
    }
}
