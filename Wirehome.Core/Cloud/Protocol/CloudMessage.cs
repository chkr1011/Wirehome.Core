using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Wirehome.Core.Cloud.Protocol
{
    public class CloudMessage
    {
        [JsonProperty("t")]
        public string Type { get; set; }

        [JsonProperty("cid")]
        public Guid? CorrelationUid { get; set; }

        [JsonProperty("p")]
        public Dictionary<string, object> Properties { get; set; }

        [JsonProperty("c")]
        public object Content { get; set; }

    }
}
