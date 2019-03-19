using Newtonsoft.Json.Linq;
using Wirehome.Core.Cloud.Protocol;

namespace Wirehome.Core.Cloud
{
    public class CloudMessageFactory
    {
        public CloudMessage CreateResponseMessage(CloudMessage requestMessage, object content)
        {
            JToken contentToken = null;
            if (content != null)
            {
                contentToken = JToken.FromObject(content);
            }

            return new CloudMessage
            {
                CorrelationUid = requestMessage.CorrelationUid,
                Content = contentToken
            };
        }

        public CloudMessage CreateMessage(string type, object content)
        {
            JToken contentToken = null;
            if (content != null)
            {
                contentToken = JToken.FromObject(content);
            }

            return new CloudMessage
            {
                Type = type,
                Content = contentToken
            };
        }
    }
}
