using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wirehome.Core.Storage;

namespace Wirehome.Core.History.Repository
{
    public partial class HistoryRepository
    {
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly StorageService _storageService;

        public TimeSpan ComponentStatusOutdatedTimeout { get; set; } = TimeSpan.FromMinutes(6);

        public HistoryRepository(StorageService storageService)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        }

        public async Task DeleteComponentStatusHistory(string componentUid, string statusUid, CancellationToken cancellationToken)
        {
            if (componentUid is null) throw new ArgumentNullException(nameof(componentUid));
            if (statusUid is null) throw new ArgumentNullException(nameof(statusUid));

            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                TryDeleteEntireDirectory(BuildComponentStatusPath(componentUid, statusUid));
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<List<HistoryValueElement>> GetComponentStatusValues(ComponentStatusFilter filter, CancellationToken cancellationToken)
        {
            var result = new List<HistoryValueElement>();

            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var rootPath = BuildComponentStatusPath(filter.ComponentUid, filter.StatusUid);
                var dayPaths = BuildDayPaths(filter.RangeStart, filter.RangeEnd);

                foreach (var dayPath in dayPaths)
                {
                    var path = Path.Combine(rootPath, dayPath.ToString(), "Values");
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
                _lock.Release();
            }

            return result;
        }

        public Task DeleteComponentStatusHistory(string componentUid, string statusUid, DateTime rangeStart, DateTime rangeEnd, CancellationToken cancellationToken)
        {
            if (componentUid is null) throw new ArgumentNullException(nameof(componentUid));
            if (statusUid is null) throw new ArgumentNullException(nameof(statusUid));


            return Task.CompletedTask;
        }

        public async Task DeleteComponentHistory(string componentUid, CancellationToken cancellationToken)
        {
            if (componentUid is null) throw new ArgumentNullException(nameof(componentUid));

            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                TryDeleteEntireDirectory(BuildComponentPath(componentUid));
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task UpdateComponentStatusValueAsync(ComponentStatusValue componentStatusValue, CancellationToken cancellationToken)
        {
            if (componentStatusValue == null) throw new ArgumentNullException(nameof(componentStatusValue));

            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);


            string path = null;
            try
            {
                path = Path.Combine(
                    BuildComponentStatusPath(componentStatusValue.ComponentUid, componentStatusValue.Value),
                    componentStatusValue.Timestamp.Year.ToString(),
                    componentStatusValue.Timestamp.Month.ToString().PadLeft(2, '0'),
                    componentStatusValue.Timestamp.Day.ToString().PadLeft(2, '0'));

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                path = Path.Combine(path, "Values");

                using (var valuesStream = new HistoryValuesStream(new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
                {
                    valuesStream.SeekEnd();

                    var createNewValue = true;

                    if (await valuesStream.MovePreviousAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var endToken = (EndToken)valuesStream.CurrentToken;

                        if (componentStatusValue.Timestamp.TimeOfDay - endToken.Value < ComponentStatusOutdatedTimeout)
                        {
                            // Update value is not outdated (the time difference is not exceeded).
                            await valuesStream.MovePreviousAsync(cancellationToken).ConfigureAwait(false);
                            var valueToken = valuesStream.CurrentToken as ValueToken;
                            await valuesStream.MoveNextAsync().ConfigureAwait(false); // Move back to end token.

                            if (string.Equals(valueToken.Value, componentStatusValue.Value, StringComparison.Ordinal))
                            {
                                // The value is still the same so we patch the end date only.
                                await valuesStream.WriteTokenAsync(new EndToken(componentStatusValue.Timestamp.TimeOfDay), cancellationToken).ConfigureAwait(false);

                                createNewValue = false;
                            }

                            await valuesStream.MoveNextAsync().ConfigureAwait(false);
                        }
                    }

                    if (createNewValue)
                    {
                        await valuesStream.WriteTokenAsync(new BeginToken(componentStatusValue.Timestamp.TimeOfDay), cancellationToken).ConfigureAwait(false);
                        await valuesStream.WriteTokenAsync(new ValueToken(componentStatusValue.Value), cancellationToken).ConfigureAwait(false);
                        await valuesStream.WriteTokenAsync(new EndToken(componentStatusValue.Timestamp.TimeOfDay), cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception exception)
            {
                // TODO: Implement automatic file repair and delete etc. when not possible.

                File.Delete(path);

                throw;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<long> GetComponentStatusHistorySize(string componentUid, string statusUid, CancellationToken cancellationToken)
        {
            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var path = BuildComponentStatusPath(componentUid, statusUid);
                return GetDirectorySize(path);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<long> GetComponentHistorySize(string componentUid, CancellationToken cancellationToken)
        {
            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var path = BuildComponentPath(componentUid);
                return GetDirectorySize(path);
            }
            finally
            {
                _lock.Release();
            }
        }

        void TryDeleteEntireDirectory(string path)
        {
            try
            {
                Directory.Delete(path, true);
            }
            catch (DirectoryNotFoundException)
            {
            }
        }

        string BuildComponentPath(string componentUid)
        {
            return Path.Combine(_storageService.DataPath, "Components", componentUid, "History");
        }

        string BuildComponentStatusPath(string componentUid, string statusUid)
        {
            return Path.Combine(BuildComponentPath(componentUid), "Status", statusUid);
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
                    Day = begin.Day
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
