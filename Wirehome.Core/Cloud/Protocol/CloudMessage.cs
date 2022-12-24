using System;
using System.Collections.Generic;
using MessagePack;

namespace Wirehome.Core.Cloud.Protocol;

[MessagePackObject]
public class CloudMessage
{
    [Key(1)]
    public string CorrelationId { get; set; }

    [Key(2)]
    public ArraySegment<byte> Payload { get; set; }

    [Key(3)]
    public Dictionary<string, string> Properties { get; set; }

    [Key(0)]
    public string Type { get; set; }
}