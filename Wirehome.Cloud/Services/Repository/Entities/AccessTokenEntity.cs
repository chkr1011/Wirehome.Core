using System;

namespace Wirehome.Cloud.Services.Repository.Entities
{
    public class AccessTokenEntity
    {
        public string Value { get; set; }

        public DateTime ValidUntil { get; set; }
    }
}
