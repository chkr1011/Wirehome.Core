using System;
using Wirehome.Core.Exceptions;

namespace Wirehome.Core.Repository.Exceptions
{
    public class WirehomeRepositoryException : WirehomeException
    {
        public WirehomeRepositoryException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}
