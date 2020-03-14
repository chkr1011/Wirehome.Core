using System;

namespace Wirehome.Core.Extensions
{
    public static class ArraySegmentExtensions
    {
        public static string ToHexString(this ReadOnlySpan<byte> source)
        {
            return BitConverter.ToString(source.ToArray());
        }

        public static string ToHexString(this ArraySegment<byte> source)
        {
            return BitConverter.ToString(source.Array, source.Offset, source.Count);
        }

        public static ArraySegment<TItem> AsArraySegment<TItem>(this TItem[] source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new ArraySegment<TItem>(source, 0, source.Length);
        }
    }
}
