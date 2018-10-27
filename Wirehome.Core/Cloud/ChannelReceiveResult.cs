using Newtonsoft.Json.Linq;

namespace Wirehome.Core.Cloud
{
    public class ChannelReceiveResult
    {
        public ChannelReceiveResult(JObject message, bool closeConnection)
        {
            Message = message;
            CloseConnection = closeConnection;
        }

        public JObject Message { get; }

        public bool CloseConnection { get; }
    }
}
