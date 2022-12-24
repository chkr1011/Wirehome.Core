using System;

namespace Wirehome.Core.History;

public class HistoryUpdateOperation
{
    public TimeSpan OldValueTimeToLive { get; set; }
    public string Path { get; set; }

    public DateTime Timestamp { get; set; }

    public object Value { get; set; }

    public HistoryValueFormatterOptions ValueFormatterOptions { get; set; }
}