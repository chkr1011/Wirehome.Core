using System;
using Wirehome.Core.Model;

namespace Wirehome.Core.MessageBus
{
    public class MessageBusInterceptor
    {
        private readonly Func<WirehomeDictionary, WirehomeDictionary> _callback;

        public MessageBusInterceptor(string uid, Func<WirehomeDictionary, WirehomeDictionary> callback)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));           
        }

        public string Uid { get; }

        public WirehomeDictionary Intercept(WirehomeDictionary message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            return _callback(message);
        }
    }
}
