using System;

namespace Wirehome.Cloud.Services
{
    public class AuthorizationScope
    {
        public AuthorizationScope(string identityUid, string channelUid)
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

        public override int GetHashCode()
        {
            return IdentityUid.GetHashCode(StringComparison.Ordinal) ^ ChannelUid.GetHashCode(StringComparison.Ordinal);
        }
    }
}