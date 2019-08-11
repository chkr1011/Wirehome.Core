
namespace Wirehome.Core.Components.Configuration
{
    public class ComponentConfiguration
    {
        public bool IsEnabled { get; set; } = true;

        public string Description { get; set; }
        
        public int InitializationPhase { get; set; }

        public ComponentLogicConfiguration Logic { get; set; }

        public string Script { get; set; }
    }
}
