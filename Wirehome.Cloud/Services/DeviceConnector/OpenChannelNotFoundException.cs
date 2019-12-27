using System;

namespace Wirehome.Cloud.Services.DeviceConnector
{
    public class OpenChannelNotFoundException : Exception
    {
        public OpenChannelNotFoundException(ChannelIdentifier identifier) 
            : base($"No open channel with ID '{identifier}' found.")
        {            
        }
    }
}
