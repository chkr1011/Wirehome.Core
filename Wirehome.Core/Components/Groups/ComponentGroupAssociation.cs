using Wirehome.Core.Foundation;

namespace Wirehome.Core.Components
{
    public class ComponentGroupAssociation
    {
        public ThreadSafeDictionary<string, object> Settings { get; set; } = new ThreadSafeDictionary<string, object>();
    }
}
