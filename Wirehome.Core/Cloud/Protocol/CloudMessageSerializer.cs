using System;
using System.IO;
using System.IO.Compression;
using MessagePack;

namespace Wirehome.Core.Cloud.Protocol;

public class CloudMessageSerializer
{
    public ArraySegment<byte> Compress(ArraySegment<byte> data)
    {
        // Do not use Brotli compression here because the drawback of the smaller size (in 
        // comparison with GZIP) is that compression takes up to 44x times of GZIP. The Pi3 will
        // require 15 seconds for just 350 KB of data!
        // We could also use GZIP but it is just DEFLATE+Checksum. Since TCP is reliable enough with
        // its own checksums we can choose DEFLATE.
        using (var outputBuffer = new MemoryStream(data.Count))
        {
            using (var inputBuffer = new MemoryStream(data.Array, data.Offset, data.Count, false))
            using (var compressor = new DeflateStream(outputBuffer, CompressionLevel.Fastest, true))
            {
                inputBuffer.CopyTo(compressor);
            }

            return new ArraySegment<byte>(outputBuffer.GetBuffer(), 0, (int)outputBuffer.Length);
        }
    }

    public ArraySegment<byte> Decompress(ArraySegment<byte> data)
    {
        using (var outputBuffer = new MemoryStream(data.Count * 2))
        {
            using (var inputBuffer = new MemoryStream(data.Array, data.Offset, data.Count, false))
            using (var decompressor = new DeflateStream(inputBuffer, CompressionMode.Decompress, false))
            {
                decompressor.CopyTo(outputBuffer);
            }

            return new ArraySegment<byte>(outputBuffer.GetBuffer(), 0, (int)outputBuffer.Length);
        }
    }

    public ArraySegment<byte> Pack<TValue>(TValue value)
    {
        if (value == null)
        {
            return null;
        }

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