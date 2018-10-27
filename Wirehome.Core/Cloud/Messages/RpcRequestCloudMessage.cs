using System;
using Newtonsoft.Json.Linq;

namespace Wirehome.Core.Cloud.Messages
{
    public class RpcRequestCloudMessage : BaseCloudMessage
    {
        public RpcRequestCloudMessage()
        {
            Type = "wirehome.cloud.message.rcp_request";
        }

        public string CorrelationUid { get; set; } = Guid.NewGuid().ToString("D");

        public JObject Message { get; set; }
    }
}
