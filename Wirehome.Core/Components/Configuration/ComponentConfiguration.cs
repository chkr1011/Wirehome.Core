
namespace Wirehome.Core.Components.Configuration
{
    public sealed class ComponentConfiguration
    {
        public bool IsEnabled { get; set; } = true;

        public int InitializationPhase { get; set; }

        public ComponentLogicConfiguration Logic { get; set; }

        public string Script { get; set; }
    }
}
