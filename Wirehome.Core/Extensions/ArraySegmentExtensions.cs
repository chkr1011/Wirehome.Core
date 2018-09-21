using System;

namespace Wirehome.Core.Extensions
{
    public static class ArraySegmentExtensions
    {
        public static string ToHexString(this ArraySegment<byte> arraySegment)
        {
            return BitConverter.ToString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
        }

        public static ArraySegment<TItem> AsArraySegment<TItem>(this TItem[] source)
        {
            return new ArraySegment<TItem>(source, 0, source.Length);
        }
    }
}
