namespace Wirehome.Core.History.Repository
{
    public partial class HistoryRepository
    {
        public class DayPath
        {
            public int Year { get; set; }

            public int Month { get; set; }

            public int Day { get; set; }

            public string Path { get; set; }
        }
    }
}
