using System;

namespace Wirehome.Core.MessageBus
{
    public class MessageBusServiceOptions
    {
        public const string Filename = "MessageBusServiceConfiguration.json";

        public int MessageProcessorsCount { get; set; } = Environment.ProcessorCount;
    }
}
