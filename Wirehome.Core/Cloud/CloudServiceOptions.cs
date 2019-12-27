using System;

namespace Wirehome.Core.Cloud
{
    public class CloudServiceOptions
    {
        public const string Filename = "CloudServiceConfiguration.json";

        public bool IsEnabled { get; set; } = true;

        public string Host { get; set; } = "wirehomecloud.azurewebsites.net";

        public string ChannelAccessToken { get; set; }

        public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(10);

        public bool UseCompression { get; set; } = true;
    }
}