using System.Collections.Generic;

namespace Wirehome.Cloud.Services.Repository.Entities
{
    public class ChannelEntity
    {
        public bool IsDefault { get; set; }

        public AccessTokenEntity AccessToken { get; set; } = new AccessTokenEntity();

        public Dictionary<string, AllowedIdentityEntity> AllowedIdentities { get; set; } = new Dictionary<string, AllowedIdentityEntity>();
    }
}
