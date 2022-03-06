using System.Collections.Generic;

namespace Wirehome.Cloud.Services.Repository.Entities;

public class IdentityEntity
{
    public Dictionary<string, ChannelEntity> Channels { get; set; } = new();

    public bool IsAdmin { get; set; }

    public bool IsLocked { get; set; }
    public string PasswordHash { get; set; }
}