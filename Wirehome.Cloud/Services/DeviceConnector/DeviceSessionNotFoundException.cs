using System;

namespace Wirehome.Cloud.Services.DeviceConnector
{
    public class DeviceSessionNotFoundException : Exception
    {
        public DeviceSessionNotFoundException(DeviceSessionIdentifier identifier) 
            : base($"Device session '{identifier}' not found.")
        {            
        }
    }
}
