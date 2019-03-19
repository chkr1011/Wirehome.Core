using Wirehome.Core.Model;

namespace Wirehome.Core.Components
{
    public class ComponentGroupAssociation
    {
        public ConcurrentWirehomeDictionary Settings { get; set; } = new ConcurrentWirehomeDictionary();
    }
}
