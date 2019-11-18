using System;
using System.Globalization;
using System.Text;
using System.Web;

namespace Wirehome.Core.History.Repository
{
    public class HistoryValueStreamSerializer
    {
        const string TimestampFormat = "HHmmss.fff";
        readonly byte _separator = (byte)' ';
        readonly byte[] _separatorBuffer;

        readonly byte[] _beginTokenKeyBuffer = Encoding.UTF8.GetBytes("b:");
        readonly byte[] _endTokenKeyBuffer = Encoding.UTF8.GetBytes("e:");
        readonly byte[] _valueTokenKeyBuffer = Encoding.UTF8.GetBytes("v:");

        public HistoryValueStreamSerializer()
        {
            _separatorBuffer = new byte[] { _separator };
        }

        public bool IsSeparator(byte source)
        {
            return source == _separator;
        }

        public byte[] SerializeTimeSpan(TimeSpan timeSpan)
        {
            // This is a workaround because .NET is not very flexible when it comes to custom TimeSpan formats.
            var buffer = new DateTime(1, 1, 1, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds, DateTimeKind.Utc).ToString(TimestampFormat);
            return Encoding.UTF8.GetBytes(buffer);
        }

        public byte[] SerializeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new byte[0];
            }

            var buffer = HttpUtility.UrlEncode(value);
            return Encoding.UTF8.GetBytes(buffer);
        }

        public byte[] SerializeSeparator()
        {
            return _separatorBuffer;
        }

        public byte[] SerializeBeginTokenKey()
        {
            return _beginTokenKeyBuffer;
        }

        public byte[] SerializeEndTokenKey()
        {
            return _endTokenKeyBuffer;
        }

        public byte[] SerializeValueTokenKey()
        {
            return _valueTokenKeyBuffer;
        }

        public Token ParseToken(ArraySegment<byte> tokenKey, ArraySegment<byte> tokenValue)
        {
            if (tokenKey.Array[tokenKey.Offset + 1] != ':')
            {
                var tokenKeyText = Encoding.UTF8.GetString(tokenKey.Array, 0, 2);
                throw new NotSupportedException($"Token is not supported ({tokenKeyText}).");
            }

            switch (tokenKey.Array[tokenKey.Offset])
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
                        var tokenKeyText = Encoding.UTF8.GetString(tokenKey.Array, 0, 2);
                        throw new NotSupportedException($"Token is not supported ({tokenKeyText}).");
                    }
            }
        }

        string ParseValue(ArraySegment<byte> source)
        {
            var buffer = Encoding.UTF8.GetString(source.Array, source.Offset, source.Count);
            return HttpUtility.UrlDecode(buffer);
        }

        TimeSpan ParseTimeSpan(ArraySegment<byte> source)
        {
            var buffer = Encoding.UTF8.GetString(source.Array, source.Offset, source.Count);
            return DateTime.ParseExact(buffer, TimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None).TimeOfDay;
        }
    }
}
