using Wirehome.Core.Exceptions;

namespace Wirehome.Core.Components.Exceptions
{
    public class ComponentNotFoundException : NotFoundException
    {
        public ComponentNotFoundException(string uid) :
            base($"Component with UID '{uid}' not found.")
        {
        }
    }
}
