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

        public HistoryValueStreamSerializer()
        {
            _separatorBuffer = new byte[] { _separator };
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

        public string ParseValue(ArraySegment<byte> source)
        {
            var buffer = Encoding.UTF8.GetString(source.Array, source.Offset, source.Count);
            return HttpUtility.UrlDecode(buffer);
        }

        public byte[] SerializeTimeSpan(TimeSpan timeSpan)
        {
            // This is a workaround because .NET is not very flexible when it comes to custom TimeSpan formats.
            var buffer = new DateTime(1, 1, 1, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds, DateTimeKind.Utc).ToString(TimestampFormat);
            return Encoding.UTF8.GetBytes(buffer);
        }

        public TimeSpan ParseTimeSpan(ArraySegment<byte> source)
        {
            var buffer = Encoding.UTF8.GetString(source.Array, source.Offset, source.Count);

            return DateTime.ParseExact(buffer, TimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None).TimeOfDay;
        }

        public bool IsSeparator(byte source)
        {
            return source == _separator;
        }

        public byte[] SerializeSeparator()
        {
            return _separatorBuffer;
        }

        internal Token ParseToken(ArraySegment<byte> tokenKey, ArraySegment<byte> tokenValue)
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
    }
}
