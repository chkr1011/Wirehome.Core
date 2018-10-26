using Wirehome.Core.Exceptions;

namespace Wirehome.Core.Components.Exceptions
{
    public class ComponentGroupNotFoundException : WirehomeException
    {
        public ComponentGroupNotFoundException(string uid) :
            base($"Component group with UID '{uid}' not found.")
        {
        }
    }
}
