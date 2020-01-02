using System;

namespace Wirehome.Core.Components
{
    public class ComponentStatusChangedEventArgs : EventArgs
    {
        public DateTime Timestamp { get; set; }

        public Component Component { get; set; }

        public string StatusUid { get; set; }

        public object OldValue { get; set; }

        public object NewValue { get; set; }
    }
}
