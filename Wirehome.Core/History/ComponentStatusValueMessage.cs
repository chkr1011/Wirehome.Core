using System;

namespace Wirehome.Core.History
{
    public class ComponentStatusValueMessage
    {
        public string ComponentUid { get; set; }
        public string StatusUid { get; set; }
        public object Value { get; set; }
        public DateTime Timestamp { get; set; }
    }
}