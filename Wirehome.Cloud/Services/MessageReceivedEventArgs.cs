using System;
using Newtonsoft.Json.Linq;

namespace Wirehome.Cloud.Services
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(JObject message)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public JObject Message { get; }
    }
}