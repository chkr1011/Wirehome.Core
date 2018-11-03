using Wirehome.Core.Exceptions;

namespace Wirehome.Core.ServiceHost.Exceptions
{
    public class ServiceNotStartedException : WirehomeException
    {
        public ServiceNotStartedException(string serviceId)
            : base($"Service with ID '{serviceId}' not started.")
        {
        }
    }
}
