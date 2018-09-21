namespace Wirehome.Core.Repositories.Exceptions
{
    public class WirehomeRepositoryEntityNotFoundException : WirehomeRepositoryException
    {
        public WirehomeRepositoryEntityNotFoundException(RepositoryEntityUid entityUid) 
            : base($"Repository entity '{entityUid}' not found.", null)
        {
        }
    }
}
