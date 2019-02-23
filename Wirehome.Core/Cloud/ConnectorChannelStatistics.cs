using System;

namespace Wirehome.Core.Cloud
{
    public class ConnectorChannelStatistics
    {
        public long BytesSent { get; set; }

        public long BytesReceived { get; set; }

        public long MalformedMessagesReceived { get; set; }

        public long MessagesSent { get; set; }

        public long MessagesReceived { get; set; }

        public long ReceiveErrors { get; set; }

        public long SendErrors { get; set; }

        public DateTime StatisticsReset { get; set; }

        public DateTime? LastMessageSent { get; set; }

        public DateTime? LastMessageReceived { get; set; }

        public DateTime Connected { get; set; }

        public TimeSpan UpTime { get; set; }
    }
}
