using System;

namespace Wirehome.Core.Components;

public sealed class ComponentStatusChangedEventArgs : EventArgs
{
    public Component Component { get; set; }

    public object NewValue { get; set; }

    public object OldValue { get; set; }

    public string StatusUid { get; set; }

    public DateTime Timestamp { get; set; }
}