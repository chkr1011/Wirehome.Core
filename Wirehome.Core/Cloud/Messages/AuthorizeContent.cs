namespace Wirehome.Core.Cloud.Messages
{
    public class AuthorizeContent
    {
        public string IdentityUid { get; set; }

        public string Password { get; set; }

        public string ChannelUid { get; set; }
    }
}
