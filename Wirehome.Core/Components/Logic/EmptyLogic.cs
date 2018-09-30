using System;
using Wirehome.Core.Components.Adapters;
using Wirehome.Core.Constants;
using Wirehome.Core.Model;

namespace Wirehome.Core.Components.Logic
{
    public class EmptyLogic : IComponentLogic
    {
        private readonly IComponentAdapter _adapter;

        public EmptyLogic(IComponentAdapter adapter)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }

        public WirehomeDictionary ProcessMessage(WirehomeDictionary parameters)
        {
            return _adapter.ProcessMessage(parameters);
        }

        public WirehomeDictionary ProcessAdapterMessage(WirehomeDictionary parameters)
        {
            // The empty logic has no logic. So it cannot do anything with incoming messages
            // from the adapter.
            return new WirehomeDictionary().WithType(ControlType.NotSupportedException);
        }
    }
}
