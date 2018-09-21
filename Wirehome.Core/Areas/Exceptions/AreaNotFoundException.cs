using Wirehome.Core.Exceptions;

namespace Wirehome.Core.Areas.Exceptions
{
    public class AreaNotFoundException : WirehomeException
    {
        public AreaNotFoundException(string uid) :
            base($"Area with UID '{uid}' not found.")
        {
        }
    }
}
