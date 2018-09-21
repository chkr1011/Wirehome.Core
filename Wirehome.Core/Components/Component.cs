using System;
using Wirehome.Core.Components.Logic;
using Wirehome.Core.Model;

namespace Wirehome.Core.Components
{
    public class Component
    {
        public Component(string uid)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
        }

        public string Uid { get; }

        public IComponentLogic Logic { get; set; }

        public WirehomeDictionary Configuration { get; } = new WirehomeDictionary();

        public WirehomeDictionary Settings { get; } = new WirehomeDictionary();

        public WirehomeDictionary Status { get; } = new WirehomeDictionary();
    }
}
