using System;

namespace Wirehome.Cloud.Services.Authorization
{
    public class DeviceAuthorizationContext
    {
        public DeviceAuthorizationContext(string identityUid, string channelUid)
        {
            IdentityUid = identityUid ?? throw new ArgumentNullException(nameof(identityUid));
            ChannelUid = channelUid ?? throw new ArgumentNullException(nameof(channelUid));
        }

        public string IdentityUid { get; }

        public string ChannelUid { get; }

        public override string ToString()
        {
            return IdentityUid;
        }
    }
}