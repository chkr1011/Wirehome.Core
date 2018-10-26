using System.Collections.Generic;

namespace Wirehome.Core.History.Extract
{
    public class HistoryExtract
    {
        public string ComponentUid { get; set; }

        public string StatusUid { get; set; }

        public int EntityCount { get; set; }

        public List<HistoryExtractDataPoint> DataPoints { get; } = new List<HistoryExtractDataPoint>();
    }
}
