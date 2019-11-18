using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        public async Task<HistoryExtract> BuildAsync(
            string componentUid,
            string statusUid,
            DateTime rangeStart,
            DateTime rangeEnd,
            TimeSpan? interval,
            HistoryExtractDataType dataType,
            int maxEntityCount,
            CancellationToken cancellationToken)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (statusUid == null) throw new ArgumentNullException(nameof(statusUid));

            var entities = await _repository.GetComponentStatusValues(new ComponentStatusFilter
            {
                ComponentUid = componentUid,
                StatusUid = statusUid,
                RangeStart = rangeStart,
                RangeEnd = rangeEnd,
                MaxEntityCount = maxEntityCount
            }, cancellationToken).ConfigureAwait(false);

            var historyExtract = new HistoryExtract
            {
                ComponentUid = componentUid,
                StatusUid = statusUid,
                EntityCount = entities.Count
            };

            if (dataType == HistoryExtractDataType.Text)
            {
                historyExtract.DataPoints.AddRange(GenerateTextBasedDataPoints(entities, rangeStart, rangeEnd));
            }
            else if (dataType == HistoryExtractDataType.Number)
            {
                historyExtract.DataPoints.AddRange(GenerateNumberBasedDataPoints(entities, rangeStart, rangeEnd, interval));
            }
            else
            {
                throw new NotSupportedException();
            }

            return historyExtract;
        }

        private static List<HistoryExtractDataPoint> GenerateTextBasedDataPoints(List<HistoryValueElement> entities, DateTime rangeStart, DateTime rangeEnd)
        {
            var dataPoints = new List<HistoryExtractDataPoint>();

            foreach (var entity in entities)
            {
                var @break = false;

                var timestamp = entity.Begin;

                if (timestamp <= rangeStart)
                {
                    // This value is outside of the range (too old). But the value is still the same when our range begins.
                    // So we adopt the value from that range and treat it as our initial value.
                    timestamp = rangeStart;
                }

                if (entity.End >= rangeEnd)
                {
                    // The value is still valid after the requested range. So we treat it as our last value.
                    // Also there is no further data point needed because we already reached the end.
                    timestamp = rangeEnd;
                    @break = true;
                }

                var dataPoint = new HistoryExtractDataPoint
                {
                    Timestamp = timestamp,
                    Value = entity.Value
                };

                dataPoints.Add(dataPoint);

                if (@break)
                {
                    break;
                }
            }

            return dataPoints;
        }

        private static IEnumerable<HistoryExtractDataPoint> GenerateNumberBasedDataPoints(
            List<HistoryValueElement> entities,
            DateTime rangeStart,
            DateTime rangeEnd,
            TimeSpan? interval)
        {
            if (!interval.HasValue)
            {
                var dataPoints = GenerateTextBasedDataPoints(entities, rangeStart, rangeEnd);

                foreach (var dataPoint in dataPoints)
                {
                    if (double.TryParse(dataPoint.Value as string,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var numberValue))
                    {
                        dataPoint.Value = numberValue;
                    }
                }

                return dataPoints;
            }

            var intervalDataPoints = new List<HistoryExtractDataPoint>();

            while (rangeStart <= rangeEnd)
            {
                // Start (real) value
                var dataPointStart = new HistoryExtractDataPoint
                {
                    Timestamp = rangeStart
                };

                dataPointStart.Value = GetValue(entities, dataPointStart.Timestamp);

                // End (real) value
                var dataPointEnd = new HistoryExtractDataPoint
                {
                    Timestamp = rangeStart.Add(interval.Value * 2)
                };

                dataPointEnd.Value = GetValue(entities, dataPointEnd.Timestamp);

                // Middle (average) value
                var dataPointMiddle = new HistoryExtractDataPoint
                {
                    Timestamp = rangeStart.Add(interval.Value),
                    Value = GetAverageValue(entities, dataPointStart.Timestamp, dataPointEnd.Timestamp)
                };

                if (!intervalDataPoints.Any())
                {
                    intervalDataPoints.Add(dataPointStart);
                }

                intervalDataPoints.Add(dataPointMiddle);
                intervalDataPoints.Add(dataPointEnd);

                rangeStart += interval.Value * 3;
            }

            return intervalDataPoints;
        }

        private static double? GetAverageValue(List<HistoryValueElement> entities, DateTime rangeStart, DateTime rangeEnd)
        {
            var value = 0D;
            var counter = 0;

            while (rangeStart < rangeEnd)
            {
                var timestampValue = GetValue(entities, rangeStart);
                if (timestampValue.HasValue)
                {
                    value += timestampValue.Value;
                    counter++;
                }

                // Use one second to allow calculation of a proper average. There is is maybe only one
                // or two real ranges with a different value. So the "larger" range must have more impact
                // than the "smaller" one. One way is to calculate percentage first or generate lots of 
                // of small second based values.
                rangeStart = rangeStart.AddSeconds(1);
            }

            if (counter == 0)
            {
                return null;
            }

            return value / counter;
        }

        private static double? GetValue(List<HistoryValueElement> entities, DateTime timestamp)
        {
            var entity = entities.FirstOrDefault(e => e.Begin <= timestamp && e.End >= timestamp);
            if (entity == null)
            {
                return null;
            }

            return Convert.ToDouble(entity.Value, CultureInfo.InvariantCulture);
        }
    }
}
