using Wirehome.Core.Foundation;

namespace Wirehome.Core.Components.Groups;

public class ComponentGroupAssociation
{
    public ThreadSafeDictionary<string, object> Settings { get; set; } = new();
}