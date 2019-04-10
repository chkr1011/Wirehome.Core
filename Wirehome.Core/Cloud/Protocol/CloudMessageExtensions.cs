using System;
using System.Text;
using MsgPack.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Wirehome.Core.Cloud.Protocol
{
    public static class CloudMessageExtensions
    {
        public static void SetContent<T>(this CloudMessage cloudMessage, T value)
        {
            if (cloudMessage == null) throw new ArgumentNullException(nameof(cloudMessage));

            if (value == null)
            {
                cloudMessage.Content = null;
            }

            if (value is JToken token)
            {
                cloudMessage.Content = new CloudMessageContent
                {
                    Data = MessagePackSerializer.Get<string>().PackSingleObject(token.ToString(Formatting.None))
                };
            }
            else
            {
                cloudMessage.Content = new CloudMessageContent
                {
                    Data = MessagePackSerializer.Get<T>().PackSingleObject(value)
                };
            }
        }

        public static T GetContent<T>(this CloudMessage cloudMessage)
        {
            if (cloudMessage == null) throw new ArgumentNullException(nameof(cloudMessage));

            if (cloudMessage.Content?.Data == null)
            {
                return default;
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)Encoding.UTF8.GetString(cloudMessage.Content.Data);
            }

            if (typeof(T) == typeof(JToken))
            {
                var json = MessagePackSerializer.Get<string>().UnpackSingleObject(cloudMessage.Content.Data.Array);
                return (T)(object)JToken.Parse(json);
            }

            return MessagePackSerializer.Get<T>().UnpackSingleObject(cloudMessage.Content.Data.Array);
        }
    }
}
