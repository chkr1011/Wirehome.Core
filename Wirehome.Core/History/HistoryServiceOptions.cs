namespace Wirehome.Core.History
{
    public sealed class HistoryServiceOptions
    {
        public const string Filename = "HistoryServiceConfiguration.json";

        public bool IsEnabled { get; set; } = false;
    }
}