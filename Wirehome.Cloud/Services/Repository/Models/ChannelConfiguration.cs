using System.Collections.Generic;

namespace Wirehome.Cloud.Services.Repository.Models
{
    public class ChannelConfiguration
    {
        public bool IsDefault { get; set; }

        public Dictionary<string, AllowedIdentityConfiguration> AllowedIdentities { get; set; } = new Dictionary<string, AllowedIdentityConfiguration>();
    }
}
