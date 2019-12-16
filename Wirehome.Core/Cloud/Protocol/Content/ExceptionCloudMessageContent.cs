using MsgPack.Serialization;

namespace Wirehome.Core.Cloud.Protocol.Content
{
    public class ExceptionCloudMessageContent
    {
        [MessagePackMember(0)]
        public string Exception { get; set; }
    }
}
