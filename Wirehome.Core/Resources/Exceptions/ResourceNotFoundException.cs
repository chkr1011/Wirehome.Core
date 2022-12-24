using Wirehome.Core.Exceptions;

namespace Wirehome.Core.Resources.Exceptions;

public class ResourceNotFoundException : WirehomeException
{
    public ResourceNotFoundException(string uid) : base($"Resource with UID '{uid}' not found.")
    {
    }
}