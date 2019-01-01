using System;
using Wirehome.Core.Constants;
using Wirehome.Core.Model;

namespace Wirehome.Core.Components.Adapters
{
    public class EmptyComponentAdapter : IComponentAdapter
    {
        public Func<WirehomeDictionary, WirehomeDictionary> MessagePublishedCallback { get; set; }

        public WirehomeDictionary ProcessMessage(WirehomeDictionary parameters)
        {
            return new WirehomeDictionary().WithType(ControlType.NotSupportedException);
        }
    }
}
