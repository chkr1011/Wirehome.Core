using Wirehome.Core.Exceptions;

namespace Wirehome.Core.Components.Exceptions;

public sealed class ComponentGroupNotFoundException : NotFoundException
{
    public ComponentGroupNotFoundException(string uid) : base($"Component group with UID '{uid}' not found.")
    {
    }
}