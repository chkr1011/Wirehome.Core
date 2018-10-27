namespace Wirehome.Core.Cloud.Messages
{
    public class AuthorizeCloudMessage : BaseCloudMessage
    {
        public AuthorizeCloudMessage()
        {
            Type = "wirehome.cloud.message.authorize";
        }

        public string IdentityUid { get; set; }

        public string Password { get; set; }

        public string ChannelUid { get; set; }
    }
}
