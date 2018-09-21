namespace Wirehome.Core.Automations.Configuration
{
    public class AutomationConfiguration
    {
        public bool IsEnabled { get; set; }

        public AutomationLogicConfiguration Logic { get; set; }
    }
}
