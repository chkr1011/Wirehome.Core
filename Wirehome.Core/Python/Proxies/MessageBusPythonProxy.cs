#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using IronPython.Runtime;
using Wirehome.Core.MessageBus;

namespace Wirehome.Core.Python.Proxies
{
    public class MessageBusPythonProxy : IPythonProxy
    {
        private readonly MessageBusService _messageBusService;

        public MessageBusPythonProxy(MessageBusService messageBusService)
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
            return _messageBusService.Subscribe(uid, filter, m => callback(PythonConvert.ToPythonDictionary(m.Message)));
        }

        public void unsubscribe(string uid)
        {
            _messageBusService.Unsubscribe(uid);
        }
    }
}