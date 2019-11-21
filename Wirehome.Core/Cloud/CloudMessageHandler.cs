using System;
using Wirehome.Core.Foundation.Model;

namespace Wirehome.Core.Cloud
{
    public class CloudMessageHandler
    {
        private readonly Func<WirehomeDictionary, WirehomeDictionary> _callback;

        public CloudMessageHandler(string type, Func<WirehomeDictionary, WirehomeDictionary> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            TargetType = type ?? throw new ArgumentNullException(nameof(type));
        }

        public string TargetType { get; }

        public WirehomeDictionary Invoke(WirehomeDictionary message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            return _callback(message);
        }
    }
}