using System;
using Wirehome.Core.Model;

namespace Wirehome.Core.Components.Adapters
{
    public class ComponentAdapterMessageReceivedEventArgs : EventArgs
    {
        public ComponentAdapterMessageReceivedEventArgs(WirehomeDictionary properties)
        {
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }

        public WirehomeDictionary Properties { get; }

        public WirehomeDictionary Result { get; } = new WirehomeDictionary();
    }
}
