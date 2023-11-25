#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

using System;
using IronPython.Runtime;
using Wirehome.Core.Python;

namespace Wirehome.Core.MessageBus;

public sealed class MessageBusServicePythonProxy : IInjectedPythonProxy
{
    public delegate void MessageCallback(PythonDictionary eventArgs);

    readonly MessageBusService _messageBusService;

    public MessageBusServicePythonProxy(MessageBusService messageBusService)
    {
        _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
    }

    public string ModuleName { get; } = "message_bus";

    public void publish(PythonDictionary message)
    {
        _messageBusService.Publish(message);
    }

    public string subscribe(string uid, PythonDictionary filter, MessageCallback callback)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        return _messageBusService.Subscribe(uid, filter, m => { callback(PythonConvert.ToPythonDictionary(m)); });
    }

    public void unsubscribe(string uid)
    {
        _messageBusService.Unsubscribe(uid);
    }
}