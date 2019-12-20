using MessagePack;
using System;
using System.IO;

namespace Wirehome.Core.Cloud.Protocol
{
    public class CloudMessageSerializer
    {
        public ArraySegment<byte> Pack<TValue>(TValue value)
        {
            if (value == null) return null;

            return MessagePackSerializer.Serialize(value);
        }

        public TValue Unpack<TValue>(ArraySegment<byte> data)
        {
            if (data.Count == 0)
            {
                return default;
            }

            using (var buffer = new MemoryStream(data.Array, data.Offset, data.Count))
            {
                return MessagePackSerializer.Deserialize<TValue>(buffer);
            }                
        }
    }
}
