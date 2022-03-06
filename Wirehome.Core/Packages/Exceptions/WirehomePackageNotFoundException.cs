using Wirehome.Core.Exceptions;

namespace Wirehome.Core.Packages.Exceptions;

public sealed class WirehomePackageNotFoundException : NotFoundException
{
    public WirehomePackageNotFoundException(PackageUid uid) : base($"Package '{uid}' not found.", null)
    {
    }
}