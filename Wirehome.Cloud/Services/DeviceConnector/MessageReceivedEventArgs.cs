using System;
using Wirehome.Core.Cloud.Messages;

namespace Wirehome.Cloud.Services.DeviceConnector
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(CloudMessage message)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public CloudMessage Message { get; }

        public bool IsHandled { get; set; }
    }
}