using System;

namespace Wirehome.Core.Discovery
{
    public class DiscoveryServiceOptions
    {
        public const string Filename = "DiscoveryServiceConfiguration.json";

        public TimeSpan SearchDuration { get; set; } = TimeSpan.FromSeconds(10);
    }
}