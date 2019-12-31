using System;

namespace Wirehome.Core.Components.History
{
    public class ComponentStatusHistoryWorkItem
    {
        public Component Component { get; set; }

        public string StatusUid { get; set; }

        public DateTime Timestamp { get; set; }

        public string Value { get; set; }
    }
}