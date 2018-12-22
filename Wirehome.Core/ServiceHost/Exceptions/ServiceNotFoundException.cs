using Wirehome.Core.Exceptions;

namespace Wirehome.Core.ServiceHost.Exceptions
{
    public class ServiceNotFoundException : NotFoundException
    {
        public ServiceNotFoundException(string serviceId) 
            : base($"Service with ID '{serviceId}' not found.")
        {
        }
    }
}
