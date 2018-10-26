using System;
using Wirehome.Core.Model;

namespace Wirehome.Core.Components.Adapters
{
    public interface IComponentAdapter
    {
        Func<WirehomeDictionary, WirehomeDictionary> MessagePublishedCallback { get; set; }

        WirehomeDictionary ProcessMessage(WirehomeDictionary message);
    }
}
