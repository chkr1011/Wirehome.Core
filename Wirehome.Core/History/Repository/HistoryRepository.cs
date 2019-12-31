using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wirehome.Core.Foundation;

namespace Wirehome.Core.History.Repository
{
    public class HistoryRepository
    {
        const string ValuesFilename = "Values";

        readonly AsyncLock _lock = new AsyncLock();
        
        public async Task<List<HistoryValueElement>> Read(HistoryReadOperation readOperation, CancellationToken cancellationToken)
        {
            if (readOperation is null) throw new ArgumentNullException(nameof(readOperation));

            var result = new List<HistoryValueElement>();

            var dayPaths = BuildDayPaths(readOperation.RangeStart, readOperation.RangeEnd);

            await _lock.EnterAsync(cancellationToken).ConfigureAwait(false);
            try
            {               
                foreach (var dayPath in dayPaths)
                {
                    var path = Path.Combine(readOperation.Path, dayPath.Path, ValuesFilename);
                    if (!File.Exists(path))
                    {
                        continue;
                    }

                    using (var fileStream = File.OpenRead(path))
                    {
                        var valueStream = new HistoryValuesStream(fileStream);

                        HistoryValueElement currentElement = null;

                        while (await valueStream.MoveNextAsync())
                        {
                            if (valueStream.CurrentToken is BeginToken beginToken)
                            {
                                currentElement = new HistoryValueElement
                                {
                                    Begin = new DateTime(
                                    dayPath.Year, dayPath.Month,
                                    dayPath.Day,
                                    beginToken.Value.Hours,
                                    beginToken.Value.Minutes,
                                    beginToken.Value.Seconds,
                                    beginToken.Value.Milliseconds,
                                    DateTimeKind.Utc)
                                };
                            }
                            else if (valueStream.CurrentToken is ValueToken valueToken)
                            {
                                currentElement.Value = valueToken.Value;
                            }
                            else if (valueStream.CurrentToken is EndToken endToken)
                            {
                                currentElement.End = new DateTime(
                                    dayPath.Year, dayPath.Month,
                                    dayPath.Day,
                                    endToken.Value.Hours,
                                    endToken.Value.Minutes,
                                    endToken.Value.Seconds,
                                    endToken.Value.Milliseconds,
                                    DateTimeKind.Utc);

                                result.Add(currentElement);
                            }
                        }
                    }
                }
            }
            finally
            {
                _lock.Exit();
            }

            return result;
        }

        public async Task Write(HistoryUpdateOperation updateOperation, CancellationToken cancellationToken)
        {
            if (updateOperation is null) throw new ArgumentNullException(nameof(updateOperation));

            var path = Path.Combine(updateOperation.Path, BuildDayPath(updateOperation.Timestamp));
            var valuesPath = Path.Combine(path, ValuesFilename);

            await _lock.EnterAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                using (var valuesStream = new HistoryValuesStream(new FileStream(valuesPath, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
                {
                    valuesStream.SeekEnd();

                    var createNewValue = true;

                    if (await valuesStream.MovePreviousAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var endToken = (EndToken)valuesStream.CurrentToken;
                        var valueIsExpired = updateOperation.Timestamp.TimeOfDay - endToken.Value > updateOperation.ValueTimeToLive;

                        if (!valueIsExpired)
                        {
                            await valuesStream.MovePreviousAsync(cancellationToken).ConfigureAwait(false);
                            var valueToken = (ValueToken)valuesStream.CurrentToken;
                            await valuesStream.MoveNextAsync().ConfigureAwait(false); // Move back to end token.

                            if (string.Equals(valueToken.Value, updateOperation.Value, StringComparison.Ordinal))
                            {
                                createNewValue = false;
                            }

                            // The end date is moved to the same value (-1 ms) as the new beginning value to fill small gaps.
                            // The 1 ms is removed to avoid wrong comparisons when begin is exactly the same as end (which is the correct value?).
                            var newEndTimestamp = updateOperation.Timestamp.TimeOfDay.Subtract(TimeSpan.FromMilliseconds(1));
                            await valuesStream.WriteTokenAsync(new EndToken(newEndTimestamp), cancellationToken).ConfigureAwait(false);

                            await valuesStream.MoveNextAsync().ConfigureAwait(false);
                        }
                    }

                    if (createNewValue)
                    {
                        await valuesStream.WriteTokenAsync(new BeginToken(updateOperation.Timestamp.TimeOfDay), cancellationToken).ConfigureAwait(false);
                        await valuesStream.WriteTokenAsync(new ValueToken(updateOperation.Value), cancellationToken).ConfigureAwait(false);
                        await valuesStream.WriteTokenAsync(new EndToken(updateOperation.Timestamp.TimeOfDay), cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                _lock.Exit();
            }
        }

        public async Task<long> GetHistorySize(string path, CancellationToken cancellationToken)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));

            await _lock.EnterAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return GetDirectorySize(path);
            }
            finally
            {
                _lock.Exit();
            }
        }

        public async Task DeleteHistory(string path, CancellationToken cancellationToken)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));

            await _lock.EnterAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                TryDeleteEntireDirectory(path);
            }
            finally
            {
                _lock.Exit();
            }
        }

        void TryDeleteEntireDirectory(string path)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));

            try
            {
                Directory.Delete(path, true);
            }
            catch (DirectoryNotFoundException)
            {
            }
        }

        static string BuildDayPath(DateTime date)
        {
            return Path.Combine(
                date.Year.ToString(),
                date.Month.ToString("00"),
                date.Day.ToString("00"));
        }

        List<DayPath> BuildDayPaths(DateTime begin, DateTime end)
        {
            var paths = new List<DayPath>();

            while (begin <= end)
            {
                paths.Add(new DayPath
                {
                    Year = begin.Year,
                    Month = begin.Month,
                    Day = begin.Day,
                    Path = BuildDayPath(begin)
                });

                begin = begin.AddDays(1);
            }

            return paths;
        }

        static long GetDirectorySize(string path)
        {
            if (!Directory.Exists(path))
            {
                return 0;
            }

            var sum = 0L;
            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                sum += new FileInfo(file).Length;
            }

            var subDirectories = Directory.GetDirectories(path);
            foreach (var subDirectory in subDirectories)
            {
                sum += GetDirectorySize(subDirectory);
            }

            return sum;
        }
    }
}