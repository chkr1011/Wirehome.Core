using System.Collections.Generic;

namespace Wirehome.Cloud.Services.Repository.Entities;

public class ChannelEntity
{
    public AccessTokenEntity AccessToken { get; set; } = new();

    public Dictionary<string, AllowedIdentityEntity> AllowedIdentities { get; set; } = new();
    public bool IsDefault { get; set; }
}