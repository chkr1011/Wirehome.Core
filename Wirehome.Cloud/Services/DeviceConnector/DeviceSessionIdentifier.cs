using System;

namespace Wirehome.Cloud.Services.DeviceConnector
{
    public class DeviceSessionIdentifier
    {
        public DeviceSessionIdentifier(string identityUid, string channelUid)
        {
            IdentityUid = identityUid ?? throw new ArgumentNullException(nameof(identityUid));
            ChannelUid = channelUid ?? throw new ArgumentNullException(nameof(channelUid));
        }

        public string IdentityUid { get; }

        public string ChannelUid { get; }
        
        public override string ToString()
        {
            return $"{IdentityUid}/{ChannelUid}";
        }
    }
}
