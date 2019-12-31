using System;

namespace Wirehome.Core.History.Repository
{
    public class HistoryReadOperation
    {
        public string Path { get; set; }

        public DateTime RangeStart { get; set; }

        public DateTime RangeEnd { get; set; }

        public int? MaxEntityCount { get; set; }
    }
}