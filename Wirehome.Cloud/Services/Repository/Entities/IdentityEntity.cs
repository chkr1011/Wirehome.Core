using System.Collections.Generic;

namespace Wirehome.Cloud.Services.Repository.Entities
{
    public class IdentityEntity
    {
        public string Uid { get; set; }

        public string PasswordHash { get; set; }

        public List<ChannelEntity> Channels { get; set; }
    }
}
