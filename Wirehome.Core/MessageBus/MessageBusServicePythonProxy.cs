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
        private readonly MessageBusService _messageBusService;

        public MessageBusServicePythonProxy(MessageBusService messageBusService)
        {
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
        }

        public string ModuleName { get; } = "message_bus";

        public void publish(PythonDictionary message)
        {
            _messageBusService.Publish(message);
        }

        //public void publish_response(PythonDictionary request, PythonDictionary response)
        //{
        //    _messageBusService.PublishResponse(request, response);
        //}

        public string subscribe(string uid, PythonDictionary filter, Action<PythonDictionary> callback)
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