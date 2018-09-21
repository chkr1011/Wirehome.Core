using System;

namespace Wirehome.Core.Exceptions
{
    public class WirehomeHardwareException : Exception
    {
        public WirehomeHardwareException(string message, int errorCode) 
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public int ErrorCode { get; }
    }
}
