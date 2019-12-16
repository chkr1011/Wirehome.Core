using MsgPack.Serialization;

namespace Wirehome.Core.Cloud.Protocol
{
    public class CloudMessageSerializer
    {
        public byte[] Pack<TValue>(TValue value)
        {
            if (value == null) return null;

            //if (value is JToken token)
            //{
            //    var json = token.ToString(Formatting.None);
            //    return MessagePackSerializer.Get<string>().PackSingleObject(json);
            //}

            return MessagePackSerializer.Get<TValue>().PackSingleObject(value);
        }

        public TValue Unpack<TValue>(byte[] data)
        {
            if (data?.Length == 0)
            {
                return default;
            }
                       
            //if (typeof(TValue) == typeof(JToken))
            //{
            //    var json = MessagePackSerializer.Get<string>().UnpackSingleObject(cloudMessage.Content.Data.Array);
            //    return (TValue)(object)JToken.Parse(json);
            //}

            return MessagePackSerializer.Get<TValue>().UnpackSingleObject(data);
        }
    }
}
