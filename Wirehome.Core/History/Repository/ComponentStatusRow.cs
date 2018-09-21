using System;

namespace Wirehome.Core.History.Repository
{
    public class ComponentStatusRow
    {
        public uint ID { get; set; }

        public string Value { get; set; }

        public DateTime Timestamp { get; set; }
    }
}