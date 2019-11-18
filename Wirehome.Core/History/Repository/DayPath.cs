using System.IO;

namespace Wirehome.Core.History.Repository
{
    public partial class HistoryRepository
    {
        public class DayPath
        {
            public int Year { get; set; }

            public int Month { get; set; }

            public int Day { get; set; }

            public override string ToString()
            {
                return Path.Combine(Year.ToString(), Month.ToString().PadLeft(2, '0'), Day.ToString().PadLeft(2, '0'));
            }
        }
    }
}
