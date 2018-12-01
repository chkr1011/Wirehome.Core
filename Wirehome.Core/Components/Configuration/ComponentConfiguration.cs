
namespace Wirehome.Core.Components.Configuration
{
    public class ComponentConfiguration
    {
        public bool IsEnabled { get; set; } = true;

        public ComponentLogicConfiguration Logic { get; set; }
    }
}
