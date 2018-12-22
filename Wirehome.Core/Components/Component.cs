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

        public ConcurrentWirehomeDictionary Configuration { get; } = new ConcurrentWirehomeDictionary();

        public ConcurrentWirehomeDictionary Settings { get; } = new ConcurrentWirehomeDictionary();

        public ConcurrentWirehomeDictionary Status { get; } = new ConcurrentWirehomeDictionary();

        public ConcurrentWirehomeHashSet<string> Tags { get; } = new ConcurrentWirehomeHashSet<string>();

        public WirehomeDictionary ProcessMessage(WirehomeDictionary message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            if (_logic == null)
            {
                throw new InvalidOperationException("A component requires a logic to process messages.");
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
