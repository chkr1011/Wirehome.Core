namespace Wirehome.Core.Components.Configuration;

public sealed class ComponentConfiguration
{
    public int InitializationPhase { get; set; }
    public bool IsEnabled { get; set; } = true;

    public ComponentLogicConfiguration Logic { get; set; }

    public string Script { get; set; }
}