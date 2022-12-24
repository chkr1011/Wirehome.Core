using System;
using System.IO;
using System.Threading.Tasks;

namespace Wirehome.Core.Extensions;

public static class StreamExtensions
{
    public static void Write(this Stream stream, byte[] buffer)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        stream.Write(buffer, 0, buffer.Length);
    }

    public static Task WriteAsync(this Stream stream, byte[] buffer)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        return stream.WriteAsync(buffer, 0, buffer.Length);
    }
}