using System;
using Wirehome.Core.Exceptions;

namespace Wirehome.Core.ServiceHost.Exceptions;

public sealed class ServiceNotFoundException : NotFoundException
{
    public ServiceNotFoundException(string serviceId, Exception innerException) : base($"Service with ID '{serviceId}' not found.", innerException)
    {
    }
}