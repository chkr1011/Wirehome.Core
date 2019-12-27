using System;

namespace Wirehome.Cloud.Services.DeviceConnector
{
    public class ChannelIdentifier
    {
        readonly int _hashCode;

        public ChannelIdentifier(string identityUid, string channelUid)
        {
            IdentityUid = identityUid ?? throw new ArgumentNullException(nameof(identityUid));
            ChannelUid = channelUid ?? throw new ArgumentNullException(nameof(channelUid));

            _hashCode = ToString().GetHashCode();
        }

        public string IdentityUid { get; }

        public string ChannelUid { get; }

        public override string ToString() => $"{IdentityUid}/{ChannelUid}";

        public override int GetHashCode() => _hashCode;

        public override bool Equals(object other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is ChannelIdentifier y)
            {
                return _hashCode.Equals(y._hashCode);
            }

            return false;
        }
    }
}
