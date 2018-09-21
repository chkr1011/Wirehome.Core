using System;

namespace Wirehome.Core.Exceptions
{
    public class WirehomeException : Exception
    {
        public WirehomeException(string message)
            : base(message, null)
        {
        }

        public WirehomeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
