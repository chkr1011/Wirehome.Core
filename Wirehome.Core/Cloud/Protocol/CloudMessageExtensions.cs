using MsgPack.Serialization;
using System;

namespace Wirehome.Core.Cloud.Protocol
{
    public class CloudMessageSerializer
    {
        public ArraySegment<byte> Pack<TValue>(TValue value)
        {
            if (value == null) return null;

            return MessagePackSerializer.Get<TValue>().PackSingleObjectAsBytes(value);
        }

        public TValue Unpack<TValue>(ArraySegment<byte> data)
        {
            if (data.Count == 0)
            {
                return default;
            }
                       
            // TODO: Use stream instead to avoid useless memory allocation.
            return MessagePackSerializer.Get<TValue>().UnpackSingleObject(data.ToArray());
        }
    }
}
