using System;
using System.Globalization;
using System.Text;
using System.Web;

namespace Wirehome.Core.History.Repository
{
    public class HistoryValueStreamSerializer
    {
        readonly byte _separator = (byte)' ';
        readonly byte[] _separatorBuffer = new byte[] { (byte)' ' };
        readonly byte[] _beginTokenIdBuffer = new byte[] { (byte)'b' };
        readonly byte[] _endTokenIdBuffer = new byte[] { (byte)'e' };
        readonly byte[] _valueTokenIdBuffer = new byte[] { (byte)'v' };

        public bool IsSeparator(byte source)
        {
            return source == _separator;
        }

        public ReadOnlySpan<byte> SerializeTimeSpan(TimeSpan timeSpan)
        {
            var buffer = timeSpan.Hours.ToString("00") +
                timeSpan.Minutes.ToString("00") +
                timeSpan.Seconds.ToString("00") +
                timeSpan.Milliseconds.ToString("000");

            return Encoding.ASCII.GetBytes(buffer).AsSpan();
        }

        public ReadOnlySpan<byte> SerializeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Array.Empty<byte>();
            }

            var buffer = HttpUtility.UrlEncode(value);
            return Encoding.UTF8.GetBytes(buffer).AsSpan();
        }

        public ReadOnlySpan<byte> SerializeSeparator()
        {
            return _separatorBuffer.AsSpan();
        }

        public ReadOnlySpan<byte> SerializeBeginTokenId()
        {
            return _beginTokenIdBuffer.AsSpan();
        }

        public ReadOnlySpan<byte> SerializeEndTokenId()
        {
            return _endTokenIdBuffer.AsSpan();
        }

        public ReadOnlySpan<byte> SerializeValueTokenId()
        {
            return _valueTokenIdBuffer.AsSpan();
        }

        public Token ParseToken(ReadOnlySpan<byte> tokenId, ReadOnlySpan<byte> tokenValue)
        {
            if (tokenId[0] == _beginTokenIdBuffer[0])
            {
                return new BeginToken(ParseTimeSpan(tokenValue));
            }

            if (tokenId[0] == _endTokenIdBuffer[0])
            {
                return new EndToken(ParseTimeSpan(tokenValue));
            }

            if (tokenId[0] == _valueTokenIdBuffer[0])
            {
                return new ValueToken(ParseValue(tokenValue));
            }

            throw new NotSupportedException($"Token ID '{(char)tokenId[0]}' is not supported.");
        }

        private string ParseValue(ReadOnlySpan<byte> source)
        {
            var buffer = Encoding.UTF8.GetString(source);
            return HttpUtility.UrlDecode(buffer);
        }

        private TimeSpan ParseTimeSpan(ReadOnlySpan<byte> source)
        {
            var buffer = Encoding.ASCII.GetString(source).AsSpan();

            return new TimeSpan(
                0,
                int.Parse(buffer.Slice(0, 2), NumberStyles.None),
                int.Parse(buffer.Slice(2, 2), NumberStyles.None),
                int.Parse(buffer.Slice(4, 2), NumberStyles.None),
                int.Parse(buffer.Slice(6, 3), NumberStyles.None));
        }
    }
}