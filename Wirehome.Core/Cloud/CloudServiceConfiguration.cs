using System;

namespace Wirehome.Core.Cloud;

public sealed class CloudServiceConfiguration
{
    public const string Filename = "CloudServiceConfiguration.json";

    public string ChannelAccessToken { get; set; }

    public string Host { get; set; } = "wirehomecloud.azurewebsites.net";

    public bool IsEnabled { get; set; } = true;

    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(10);

    public bool UseCompression { get; set; } = true;
}