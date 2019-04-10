using Wirehome.Core.Cloud.Protocol;

namespace Wirehome.Core.Cloud.Channel
{
    public class ConnectorChannelReceiveResult
    {
        public ConnectorChannelReceiveResult(CloudMessage message, bool closeConnection)
        {
            Message = message;
            CloseConnection = closeConnection;
        }

        public CloudMessage Message { get; }

        public bool CloseConnection { get; }
    }
}
