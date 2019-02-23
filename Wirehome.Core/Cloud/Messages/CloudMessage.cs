using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Wirehome.Core.Cloud.Messages
{
    public class CloudMessage
    {
        public string Type { get; set; }

        public Guid? CorrelationUid { get; set; }

        public Dictionary<string, object> Properties { get; set; }

        public JToken Content { get; set; }
    }
}
