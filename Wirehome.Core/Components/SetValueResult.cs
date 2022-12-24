namespace Wirehome.Core.Components;

public sealed class SetValueResult
{
    public bool IsNewValue { get; set; }
    public object OldValue { get; set; }
}