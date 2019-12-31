using System;

namespace Wirehome.Core.History.Repository
{
    public class HistoryUpdateOperation
    {
        public string Path { get; set; }

        public DateTime Timestamp { get; set; }

        public string Value { get; set; }

        public TimeSpan ValueTimeToLive { get; set; }

        public HistoryValueFormatterOptions ValueFormatterOptions { get; set; }
    }
}