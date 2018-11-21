namespace Wirehome.Core.Exceptions
{
    public class HardwareException : WirehomeException
    {
        public HardwareException(string message, int errorCode) 
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public int ErrorCode { get; }
    }
}
