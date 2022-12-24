using Wirehome.Core.Exceptions;

namespace Wirehome.Core.Devices.Exceptions;

public class DeviceNotFoundException : NotFoundException
{
    public DeviceNotFoundException(string uid) : base($"Device with UID '{uid}' not found.")
    {
    }
}