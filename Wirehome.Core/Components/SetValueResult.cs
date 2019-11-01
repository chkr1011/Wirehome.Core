namespace Wirehome.Core.Components
{
    public partial class Component
    {
        public class SetValueResult
        {
            public object OldValue { get; set; }

            public bool IsNewValue { get; set; }
        }
    }
}
