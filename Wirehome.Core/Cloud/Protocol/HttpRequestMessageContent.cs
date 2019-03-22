using System.Collections.Generic;
using Newtonsoft.Json;

namespace Wirehome.Core.Cloud.Protocol
{
    public class HttpRequestMessageContent
    {
        [JsonProperty("m")]
        public string Method { get; set; }

        [JsonProperty("u")]
        public string Uri { get; set; }

        [JsonProperty("h")]
        public Dictionary<string, string> Headers { get; set; }

        [JsonProperty("c")]
        public byte[] Content { get; set; }
    }
}
