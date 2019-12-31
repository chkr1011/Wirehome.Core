using System;

namespace Wirehome.Core.History
{
    public class HistoryRepositoryUpdateOperation
    {
        public string Path { get; set; }

        public DateTime Timestamp { get; set; }

        public string Value { get; set; }

        public TimeSpan OldValueTimeToLive { get; set; }
    }
}