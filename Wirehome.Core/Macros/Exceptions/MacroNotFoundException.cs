using Wirehome.Core.Exceptions;

namespace Wirehome.Core.Macros.Exceptions;

public class MacroNotFoundException : NotFoundException
{
    public MacroNotFoundException(string uid) : base($"Macro with UID '{uid}' not found.")
    {
    }
}