using System.Collections.Generic;

namespace Wirehome.Cloud.Services.Repository.Models
{
    public class IdentityConfiguration
    {
        public string PasswordHash { get; set; }

        public bool IsLocked { get; set; }

        public bool IsAdmin { get; set; }

        public string DefaultChannel { get; set; }

        public Dictionary<string, ChannelConfiguration> Channels { get; set; } = new Dictionary<string, ChannelConfiguration>();
    }
}
