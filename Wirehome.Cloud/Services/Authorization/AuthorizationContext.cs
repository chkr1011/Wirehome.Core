using System;

namespace Wirehome.Cloud.Services.Authorization
{
    public class AuthorizationContext
    {
        public AuthorizationContext(string identityUid, string channelUid)
        {
            IdentityUid = identityUid ?? throw new ArgumentNullException(nameof(identityUid));
            ChannelUid = channelUid ?? throw new ArgumentNullException(nameof(channelUid));

            IdentityUid = IdentityUid.ToLowerInvariant();
        }

        public string IdentityUid { get; }

        public string ChannelUid { get; }

        public override string ToString()
        {
            return $"{IdentityUid}/{ChannelUid}";
        }
    }
}