using System;

namespace Wirehome.Cloud.Services.DeviceConnector;

public sealed class ChannelIdentifier
{
    readonly int _hashCode;

    public ChannelIdentifier(string identityUid, string channelUid)
    {
        IdentityUid = identityUid ?? throw new ArgumentNullException(nameof(identityUid));
        ChannelUid = channelUid ?? throw new ArgumentNullException(nameof(channelUid));

        _hashCode = ToString().GetHashCode();
    }

    public string ChannelUid { get; }

    public string IdentityUid { get; }

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

    public override int GetHashCode()
    {
        return _hashCode;
    }

    public override string ToString()
    {
        return $"{IdentityUid}/{ChannelUid}";
    }
}