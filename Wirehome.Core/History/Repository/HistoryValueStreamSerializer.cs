using System;
using System.Globalization;
using System.Text;
using System.Web;

namespace Wirehome.Core.History.Repository
{
    public class HistoryValueStreamSerializer
    {
        readonly byte _separator = (byte)' ';
        readonly byte[] _separatorBuffer;

        readonly byte[] _beginTokenKeyBuffer = Encoding.ASCII.GetBytes("b:");
        readonly byte[] _endTokenKeyBuffer = Encoding.ASCII.GetBytes("e:");
        readonly byte[] _valueTokenKeyBuffer = Encoding.ASCII.GetBytes("v:");

        public HistoryValueStreamSerializer()
        {
            _separatorBuffer = new byte[] { _separator };
        }

        public bool IsSeparator(byte source)
        {
            return source == _separator;
        }

        public ReadOnlySpan<byte> SerializeTimeSpan(TimeSpan timeSpan)
        {
            var buffer = timeSpan.Hours.ToString("00") +
                timeSpan.Minutes.ToString("00") +
                timeSpan.Seconds.ToString("00") +
                "." +
                timeSpan.Milliseconds.ToString("000");

            return Encoding.ASCII.GetBytes(buffer).AsSpan();
        }

        public ReadOnlySpan<byte> SerializeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new byte[0];
            }

            var buffer = HttpUtility.UrlEncode(value);
            return Encoding.UTF8.GetBytes(buffer).AsSpan();
        }

        public ReadOnlySpan<byte> SerializeSeparator()
        {
            return _separatorBuffer.AsSpan();
        }

        public ReadOnlySpan<byte> SerializeBeginTokenKey()
        {
            return _beginTokenKeyBuffer.AsSpan();
        }

        public ReadOnlySpan<byte> SerializeEndTokenKey()
        {
            return _endTokenKeyBuffer.AsSpan();
        }

        public ReadOnlySpan<byte> SerializeValueTokenKey()
        {
            return _valueTokenKeyBuffer.AsSpan();
        }

        public Token ParseToken(ReadOnlySpan<byte> tokenKey, ReadOnlySpan<byte> tokenValue)
        {
            if (tokenKey[1] != ':')
            {
                var tokenKeyText = Encoding.UTF8.GetString(tokenKey);
                throw new NotSupportedException($"Token is not supported ({tokenKeyText}).");
            }

            switch (tokenKey[0])
            {
                case (byte)'b':
                    {
                        return new BeginToken(ParseTimeSpan(tokenValue));
                    }

                case (byte)'v':
                    {
                        return new ValueToken(ParseValue(tokenValue));
                    }

                case (byte)'e':
                    {
                        return new EndToken(ParseTimeSpan(tokenValue));
                    }

                default:
                    {
                        var tokenKeyText = Encoding.UTF8.GetString(tokenKey);
                        throw new NotSupportedException($"Token is not supported ({tokenKeyText}).");
                    }
            }
        }

        string ParseValue(ReadOnlySpan<byte> source)
        {
            var buffer = Encoding.UTF8.GetString(source);
            return HttpUtility.UrlDecode(buffer);
        }

        TimeSpan ParseTimeSpan(ReadOnlySpan<byte> source)
        {
            var buffer = Encoding.ASCII.GetString(source).AsSpan();

            return new TimeSpan(
                0,
                int.Parse(buffer.Slice(0, 2), NumberStyles.Integer),
                int.Parse(buffer.Slice(2, 2), NumberStyles.Integer),
                int.Parse(buffer.Slice(4, 2), NumberStyles.Integer),
                int.Parse(buffer.Slice(7, 3), NumberStyles.Integer));
        }
    }
}
