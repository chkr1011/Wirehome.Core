namespace Wirehome.Core.Repository.Exceptions
{
    public class WirehomeRepositoryPackageNotFoundException : WirehomeRepositoryException
    {
        public WirehomeRepositoryPackageNotFoundException(PackageUid packageUid) 
            : base($"Package '{packageUid}' not found.", null)
        {
        }
    }
}
