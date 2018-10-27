using System;
using Newtonsoft.Json.Linq;

namespace Wirehome.Core.Cloud.Messages
{
    public class CloudMessageParser
    {
        public bool TryParse<TMessage>(JObject source, out TMessage message) where TMessage : BaseCloudMessage, new()
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            message = source.ToObject<TMessage>();
            var checkInstance = Activator.CreateInstance<TMessage>();

            if (message.Type != checkInstance.Type)
            {
                message = null;
                return false;
            }

            return true;
        }
    }
}
