using System.Collections.Generic;

namespace Wirehome.Core.History.Extract;

public class HistoryExtract
{
    public List<HistoryExtractDataPoint> DataPoints { get; } = new();

    public int EntityCount { get; set; }
    public string Path { get; set; }
}