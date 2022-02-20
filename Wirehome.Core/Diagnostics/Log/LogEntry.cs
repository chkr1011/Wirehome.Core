using System;
using MessagePack;
using Microsoft.Extensions.Logging;

namespace Wirehome.Core.Diagnostics.Log;

[MessagePackObject]
public sealed class LogEntry
{
    [Key(0)]
    public string Exception { get; set; }

    [Key(1)]
    public LogLevel Level { get; set; }

    [Key(2)]
    public string Message { get; set; }

    [Key(3)]
    public string Source { get; set; }

    [Key(4)]
    public DateTime Timestamp { get; set; }
}