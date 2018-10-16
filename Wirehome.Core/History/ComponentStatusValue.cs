using System;

namespace Wirehome.Core.History
{
    public class ComponentStatusValue
    {
        public string ComponentUid { get; set; }
        public string StatusUid { get; set; }
        public string Value { get; set; }
        public DateTime Timestamp { get; set; }
    }
}