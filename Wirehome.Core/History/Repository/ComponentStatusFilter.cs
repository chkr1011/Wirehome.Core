using System;

namespace Wirehome.Core.History.Repository
{
    public class ComponentStatusFilter
    {
        public string ComponentUid { get; set; }

        public string StatusUid { get; set; }

        public DateTime RangeStart { get; set; }

        public DateTime RangeEnd { get; set; }

        public int? MaxEntityCount { get; set; }
    }
}
