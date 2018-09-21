using System;
using Wirehome.Core.Exceptions;

namespace Wirehome.Core.Repositories.Exceptions
{
    public class WirehomeRepositoryException : WirehomeException
    {
        public WirehomeRepositoryException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}
