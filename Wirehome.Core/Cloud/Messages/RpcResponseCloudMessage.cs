using Newtonsoft.Json.Linq;

namespace Wirehome.Core.Cloud.Messages
{
    public class RpcResponseCloudMessage : BaseCloudMessage
    {
        public RpcResponseCloudMessage()
        {
            Type = "wirehome.cloud.message.rcp_response";
        }

        public string CorrelationUid { get; set; }

        public JObject Message { get; set; }
    }
}
