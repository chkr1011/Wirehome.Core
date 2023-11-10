using System;

namespace Wirehome.Core.Extensions;

public static class ArraySegmentExtensions
{
    public static ArraySegment<TItem> AsArraySegment<TItem>(this TItem[] source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return new ArraySegment<TItem>(source, 0, source.Length);
    }

    public static string ToHexString(this ReadOnlySpan<byte> source)
    {
        return BitConverter.ToString(source.ToArray());
    }

    public static string ToHexString(this byte[] source, int length)
    {
        return BitConverter.ToString(source, 0, length);
    }
}