using System;
using System.Collections.Concurrent;
using Wirehome.Core.Model;

namespace Wirehome.Core.Components
{
    public class ComponentGroup
    {
        public ComponentGroup(string uid)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
        }

        public string Uid { get; }

        public WirehomeDictionary Settings { get; } = new WirehomeDictionary();

        public WirehomeDictionary Status { get; } = new WirehomeDictionary();

        public ConcurrentDictionary<string, ComponentGroupAssociation> Components { get; } = new ConcurrentDictionary<string, ComponentGroupAssociation>();

        public ConcurrentDictionary<string, ComponentGroupAssociation> Macros { get; } = new ConcurrentDictionary<string, ComponentGroupAssociation>();
    }
}
