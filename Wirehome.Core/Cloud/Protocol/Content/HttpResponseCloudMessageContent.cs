using System.Collections.Generic;
using MessagePack;

namespace Wirehome.Core.Cloud.Protocol.Content;

[MessagePackObject]
public class HttpResponseCloudMessageContent
{
    [Key(2)]
    public byte[] Content { get; set; }

    [Key(1)]
    public Dictionary<string, string> Headers { get; set; }

    [Key(0)]
    public int StatusCode { get; set; }
}