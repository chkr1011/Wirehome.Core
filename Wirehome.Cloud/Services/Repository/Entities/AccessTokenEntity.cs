using System;

namespace Wirehome.Cloud.Services.Repository.Entities;

public class AccessTokenEntity
{
    public DateTime ValidUntil { get; set; }
    public string Value { get; set; }
}