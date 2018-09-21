using System;
using Wirehome.Core.Components.Adapters;
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

        public WirehomeDictionary ExecuteCommand(WirehomeDictionary parameters)
        {
            return _adapter.SendMessage(parameters);
        }
    }
}
