using System;
using System.Globalization;
using System.Linq;
using Wirehome.Core.History.Repository;

namespace Wirehome.Core.History.Extract
{
    public class HistoryExtractBuilder
    {
        private readonly HistoryRepository _repository;

        public HistoryExtractBuilder(HistoryRepository historyRepository)
        {
            _repository = historyRepository ?? throw new ArgumentNullException(nameof(historyRepository));
        }

        public HistoryExtract Build(string componentUid, string statusUid, DateTimeOffset rangeStart, DateTimeOffset rangeEnd, TimeSpan interval, HistoryExtractDataType dataType)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (statusUid == null) throw new ArgumentNullException(nameof(statusUid));

            var entities = _repository.GetComponentStatusValues(componentUid, statusUid, rangeStart, rangeEnd);

            var historyExtract = new HistoryExtract
            {
                ComponentUid = componentUid,
                StatusUid = statusUid,
                EntityCount = entities.Count
            };

            while (rangeStart <= rangeEnd)
            {
                var dataPoint = new HistoryExtractDataPoint
                {
                    Timestamp = rangeStart
                };

                var entity = entities.FirstOrDefault(e => e.RangeStart <= dataPoint.Timestamp && e.RangeEnd >= dataPoint.Timestamp);
                if (entity != null)
                {
                    if (dataType == HistoryExtractDataType.Number)
                    {
                        dataPoint.Value = Convert.ToDouble(entity.Value, CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        dataPoint.Value = entity.Value;
                    }
                }

                historyExtract.DataPoints.Add(dataPoint);

                rangeStart += interval;
            }

            return historyExtract;
        }
    }
}
