using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Wirehome.Core.Cloud.Protocol
{
    public class CloudMessage
    {
        public string Type { get; set; }

        public Guid? CorrelationUid { get; set; }

        public Dictionary<string, object> Properties { get; set; }

        public JToken Content { get; set; }
    }
}
