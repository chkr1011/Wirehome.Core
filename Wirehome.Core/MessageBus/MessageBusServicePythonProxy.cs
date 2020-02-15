#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using IronPython.Runtime;
using System;
using Wirehome.Core.Python;

namespace Wirehome.Core.MessageBus
{
    public class MessageBusServicePythonProxy : IInjectedPythonProxy
    {
        readonly MessageBusService _messageBusService;

        public MessageBusServicePythonProxy(MessageBusService messageBusService)
        {
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
        }

        public string ModuleName { get; } = "message_bus";

        public delegate void MessageCallback(PythonDictionary eventArgs);

        public void publish(PythonDictionary message)
        {
            _messageBusService.Publish(message);
        }

        public string subscribe(string uid, PythonDictionary filter, MessageCallback callback)
        {
            return _messageBusService.Subscribe(uid, filter, m =>
            {
                var pythonDictionary = PythonConvert.ToPythonDictionary(m.Message);
                pythonDictionary["subscription_uid"] = uid;

                callback(pythonDictionary);
            });
        }

        public void unsubscribe(string uid)
        {
            _messageBusService.Unsubscribe(uid);
        }
    }
}