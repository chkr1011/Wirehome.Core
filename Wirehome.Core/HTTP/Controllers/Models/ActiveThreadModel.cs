namespace Wirehome.Core.HTTP.Controllers.Models
{
    public sealed class ActiveThreadModel
    {
        public string CreatedTimestamp { get; set; }

        public int Uptime { get; set; }

        public int ManagedThreadId { get; set; }
    }
}
