using System.Collections.Generic;

namespace Wirehome.Cloud.Services.Repository.Entities
{
    public class IdentityEntity
    {
        public string PasswordHash { get; set; }

        public bool IsLocked { get; set; }

        public bool IsAdmin { get; set; }

        public Dictionary<string, ChannelEntity> Channels { get; set; } = new Dictionary<string, ChannelEntity>();
    }
}
