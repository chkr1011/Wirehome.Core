namespace Wirehome.Core.Diagnostics.Log
{
    public class LogEntryFilter
    {
        public bool IncludeInformation { get; set; }

        public bool IncludeWarnings { get; set; }

        public bool IncludeErrors { get; set; }

        public int TakeCount { get; set; }
    }
}