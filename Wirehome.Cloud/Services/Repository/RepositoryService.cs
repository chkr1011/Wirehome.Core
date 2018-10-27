using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Wirehome.Cloud.Services.Repository.Entities;

namespace Wirehome.Cloud.Services.Repository
{
    public class RepositoryService
    {
        public List<IdentityEntity> GetIdentities()
        {
            return new List<IdentityEntity>
            {
                new IdentityEntity
                {
                    Uid = "christian.kratky@googlemail.com",
                    PasswordHash = new PasswordHasher<string>().HashPassword("christian.kratky@googlemail.com", "topSecretPassword123"),
                    Channels = new List<ChannelEntity>
                    {
                        new ChannelEntity
                        {
                            Uid = "default"
                        }
                    }
                    
                }
            };
        }
    }
}
