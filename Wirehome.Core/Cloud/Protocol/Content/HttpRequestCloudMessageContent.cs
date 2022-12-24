using System;
using System.Collections.Generic;
using MessagePack;

namespace Wirehome.Core.Cloud.Protocol.Content;

[MessagePackObject]
public class HttpRequestCloudMessageContent
{
    [Key(3)]
    public ArraySegment<byte> Content { get; set; }

    [Key(2)]
    public Dictionary<string, string> Headers { get; set; }

    [Key(0)]
    public string Method { get; set; }

    [Key(1)]
    public string Uri { get; set; }
}