using System;

namespace Wirehome.Core.Discovery
{
    public class SsdpDevice
    {
        public string Usn { get; set; }

        public string DeviceType { get; set; }

        public string DeviceTypeNamespace { get; set; }

        public string DescriptionLocation { get; set; }

        public string NotificationType { get; set; }

        public TimeSpan CacheLifetime { get; set; }
    }
}
