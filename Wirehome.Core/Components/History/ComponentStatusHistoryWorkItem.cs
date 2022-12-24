using System;

namespace Wirehome.Core.Components.History;

public sealed class ComponentStatusHistoryWorkItem
{
    public Component Component { get; set; }

    public string StatusUid { get; set; }
    public DateTime Timestamp { get; set; }

    public object Value { get; set; }
}