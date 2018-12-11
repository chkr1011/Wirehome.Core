using Wirehome.Core.Exceptions;

namespace Wirehome.Core.Packages.Exceptions
{
    public class WirehomePackageNotFoundException : NotFoundException
    {
        public WirehomePackageNotFoundException(PackageUid uid) 
            : base($"Package '{uid}' not found.", null)
        {
        }
    }
}
