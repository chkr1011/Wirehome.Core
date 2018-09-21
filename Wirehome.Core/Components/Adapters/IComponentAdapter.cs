using System;
using Wirehome.Core.Model;

namespace Wirehome.Core.Components.Adapters
{
    public interface IComponentAdapter
    {
        event EventHandler<ComponentAdapterMessageReceivedEventArgs> MessageReceived;

        WirehomeDictionary SendMessage(WirehomeDictionary parameters);
    }
}
