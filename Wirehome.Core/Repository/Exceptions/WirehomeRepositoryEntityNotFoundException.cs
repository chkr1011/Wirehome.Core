namespace Wirehome.Core.Repository.Exceptions
{
    public class WirehomeRepositoryEntityNotFoundException : WirehomeRepositoryException
    {
        public WirehomeRepositoryEntityNotFoundException(RepositoryEntityUid entityUid) 
            : base($"Repository entity '{entityUid}' not found.", null)
        {
        }
    }
}
