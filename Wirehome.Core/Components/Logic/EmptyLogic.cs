using System;
using Wirehome.Core.Components.Adapters;
using Wirehome.Core.Constants;
using Wirehome.Core.Foundation.Model;

namespace Wirehome.Core.Components.Logic
{
    public class EmptyComponentLogic : IComponentLogic
    {
        private readonly IComponentAdapter _adapter;

        public EmptyComponentLogic(IComponentAdapter adapter)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }

        public WirehomeDictionary ProcessMessage(WirehomeDictionary parameters)
        {
            return _adapter.ProcessMessage(parameters);
        }

        public WirehomeDictionary GetDebugInformation(WirehomeDictionary parameters)
        {
            return new WirehomeDictionary().WithType(ControlType.NotSupportedException);
        }
    }
}
