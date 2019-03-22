using System.Collections.Generic;
using Newtonsoft.Json;

namespace Wirehome.Core.Cloud.Protocol
{
    public class HttpResponseMessageContent
    {
        [JsonProperty("s")]
        public int? StatusCode { get; set; }

        [JsonProperty("h")]
        public Dictionary<string, string> Headers { get; set; }

        [JsonProperty("c")]
        public byte[] Content { get; set; }
    }
}
