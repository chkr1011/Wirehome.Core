using System;

namespace Wirehome.Core.Cloud
{
    public class CloudServiceOptions
    {
        public const string Filename = "CloudServiceConfiguration.json";

        public bool IsEnabled { get; set; } = true;

        public string Host { get; set; } = "wirehomecloud.azurewebsites.net";

        public string IdentityUid { get; set; }

        public string Password { get; set; }

        public string ChannelUid { get; set; } = "default";

        public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(10);
    }
}
