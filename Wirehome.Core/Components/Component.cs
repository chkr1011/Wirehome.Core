using System;
using Wirehome.Core.Components.Logic;
using Wirehome.Core.Model;

namespace Wirehome.Core.Components
{
    public class Component
    {
        private IComponentLogic _logic;

        public Component(string uid)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
        }

        public string Uid { get; }

        public WirehomeDictionary Configuration { get; } = new WirehomeDictionary();

        public WirehomeDictionary Settings { get; } = new WirehomeDictionary();

        public WirehomeDictionary Status { get; } = new WirehomeDictionary();

        public WirehomeDictionary ProcessMessage(WirehomeDictionary message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            if (_logic == null)
            {
                throw new InvalidOperationException("A component requires a logic to perform operations.");
            }

            return _logic.ProcessMessage(message);
        }

        public void SetLogic(IComponentLogic logic)
        {
            if (_logic != null)
            {
                throw new InvalidOperationException("A component logic cannot be changed.");
            }

            _logic = logic ?? throw new ArgumentNullException(nameof(logic));
        }
    }
}
