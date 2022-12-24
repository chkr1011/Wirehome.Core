using MessagePack;

namespace Wirehome.Core.Cloud.Protocol.Content;

[MessagePackObject]
public class ExceptionCloudMessageContent
{
    [Key(0)]
    public string Exception { get; set; }
}