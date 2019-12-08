using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wirehome.Core.Components;
using Wirehome.Core.Foundation;
using Wirehome.Core.Storage;

namespace Wirehome.Core.History.Repository
{
    public class HistoryRepository
    {
        private const string ValuesFilename = "Values";

        private readonly AsyncLock _lock = new AsyncLock();
        private readonly StorageService _storageService;
        private readonly ComponentRegistryService _componentRegistryService;

        public TimeSpan ComponentStatusOutdatedTimeout { get; set; } = TimeSpan.FromMinutes(6);

        public HistoryRepository(StorageService storageService, ComponentRegistryService componentRegistryService)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
        }

        public async Task<List<HistoryValueElement>> GetComponentStatusValues(ComponentStatusFilter filter, CancellationToken cancellationToken)
        {
            var result = new List<HistoryValueElement>();

            await _lock.EnterAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var rootPath = BuildComponentStatusPath(filter.ComponentUid, filter.StatusUid);
                var dayPaths = BuildDayPaths(filter.RangeStart, filter.RangeEnd);

                foreach (var dayPath in dayPaths)
                {
                    var path = Path.Combine(rootPath, dayPath.Path, ValuesFilename);
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

        public async Task Update(HistoryUpdate update, CancellationToken cancellationToken)
        {
            if (update is null) throw new ArgumentNullException(nameof(update));

            await _lock.EnterAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var path = Path.Combine(
                    _storageService.DataPath,
                    "History",
                    update.Path,
                    BuildDayPath(update.Timestamp));

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                path = Path.Combine(path, ValuesFilename);

                using (var valuesStream = new HistoryValuesStream(new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
                {
                    valuesStream.SeekEnd();

                    var createNewValue = true;

                    if (await valuesStream.MovePreviousAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var endToken = (EndToken)valuesStream.CurrentToken;
                        var valueIsExpired = update.Timestamp.TimeOfDay - endToken.Value > ComponentStatusOutdatedTimeout;

                        if (!valueIsExpired)
                        {
                            await valuesStream.MovePreviousAsync(cancellationToken).ConfigureAwait(false);
                            var valueToken = (ValueToken)valuesStream.CurrentToken;
                            await valuesStream.MoveNextAsync().ConfigureAwait(false); // Move back to end token.

                            if (string.Equals(valueToken.Value, update.Value, StringComparison.Ordinal))
                            {
                                createNewValue = false;
                            }

                            // The end date is moved to the same value (-1 ms) as the new beginning value to fill small gaps.
                            // The 1 ms is removed to avoid wrong comparisons when begin is exactly the same as end (which is the correct value?).
                            var newEndTimestamp = update.Timestamp.TimeOfDay.Subtract(TimeSpan.FromMilliseconds(1));
                            await valuesStream.WriteTokenAsync(new EndToken(newEndTimestamp), cancellationToken).ConfigureAwait(false);

                            await valuesStream.MoveNextAsync().ConfigureAwait(false);
                        }
                    }

                    if (createNewValue)
                    {
                        await valuesStream.WriteTokenAsync(new BeginToken(update.Timestamp.TimeOfDay), cancellationToken).ConfigureAwait(false);
                        await valuesStream.WriteTokenAsync(new ValueToken(update.Value), cancellationToken).ConfigureAwait(false);
                        await valuesStream.WriteTokenAsync(new EndToken(update.Timestamp.TimeOfDay), cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                _lock.Exit();
            }
        }

        public async Task UpdateComponentStatusValueAsync(ComponentStatusValue componentStatusValue, CancellationToken cancellationToken)
        {
            if (componentStatusValue == null) throw new ArgumentNullException(nameof(componentStatusValue));

            await _lock.EnterAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var path = Path.Combine(
                    BuildComponentStatusPath(componentStatusValue.ComponentUid, componentStatusValue.StatusUid),
                    BuildDayPath(componentStatusValue.Timestamp));

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                path = Path.Combine(path, ValuesFilename);

                using (var valuesStream = new HistoryValuesStream(new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
                {
                    valuesStream.SeekEnd();

                    var createNewValue = true;

                    if (await valuesStream.MovePreviousAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var endToken = (EndToken)valuesStream.CurrentToken;
                        var valueIsExpired = componentStatusValue.Timestamp.TimeOfDay - endToken.Value > ComponentStatusOutdatedTimeout;

                        if (!valueIsExpired)
                        {
                            await valuesStream.MovePreviousAsync(cancellationToken).ConfigureAwait(false);
                            var valueToken = (ValueToken)valuesStream.CurrentToken;
                            await valuesStream.MoveNextAsync().ConfigureAwait(false); // Move back to end token.

                            if (string.Equals(valueToken.Value, componentStatusValue.Value, StringComparison.Ordinal))
                            {
                                createNewValue = false;
                            }

                            // The end date is moved to the same value (-1 ms) as the new beginning value to fill small gaps.
                            // The 1 ms is removed to avoid wrong comparisons when begin is exactly the same as end (which is the correct value?).
                            var newEndTimestamp = componentStatusValue.Timestamp.TimeOfDay.Subtract(TimeSpan.FromMilliseconds(1));
                            await valuesStream.WriteTokenAsync(new EndToken(newEndTimestamp), cancellationToken).ConfigureAwait(false);

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
            finally
            {
                _lock.Exit();
            }
        }

        public async Task<long> GetComponentStatusHistorySize(string componentUid, string statusUid, CancellationToken cancellationToken)
        {
            await _lock.EnterAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var path = BuildComponentStatusPath(componentUid, statusUid);
                return GetDirectorySize(path);
            }
            finally
            {
                _lock.Exit();
            }
        }

        public async Task<long> GetComponentHistorySize(string componentUid, CancellationToken cancellationToken)
        {
            await _lock.EnterAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var path = BuildComponentPath(componentUid);
                return GetDirectorySize(path);
            }
            finally
            {
                _lock.Exit();
            }
        }

        public async Task DeleteEntireHistory(CancellationToken cancellationToken)
        {
            await _lock.EnterAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                foreach (var component in _componentRegistryService.GetComponents())
                {
                    TryDeleteEntireDirectory(BuildComponentPath(component.Uid));
                }
            }
            finally
            {
                _lock.Exit();
            }
        }

        public async Task DeleteComponentStatusHistory(string componentUid, string statusUid, CancellationToken cancellationToken)
        {
            if (componentUid is null) throw new ArgumentNullException(nameof(componentUid));
            if (statusUid is null) throw new ArgumentNullException(nameof(statusUid));

            await _lock.EnterAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                TryDeleteEntireDirectory(BuildComponentStatusPath(componentUid, statusUid));
            }
            finally
            {
                _lock.Exit();
            }
        }

        public async Task DeleteComponentHistory(string componentUid, CancellationToken cancellationToken)
        {
            if (componentUid is null) throw new ArgumentNullException(nameof(componentUid));

            await _lock.EnterAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                TryDeleteEntireDirectory(BuildComponentPath(componentUid));
            }
            finally
            {
                _lock.Exit();
            }
        }

        private static string BuildDayPath(DateTime date)
        {
            return Path.Combine(
                date.Year.ToString(),
                date.Month.ToString("00"),
                date.Day.ToString("00"));
        }

        private void TryDeleteEntireDirectory(string path)
        {
            try
            {
                Directory.Delete(path, true);
            }
            catch (DirectoryNotFoundException)
            {
            }
        }

        private string BuildComponentPath(string componentUid)
        {
            return Path.Combine(_storageService.DataPath, "Components", componentUid, "History");
        }

        private string BuildComponentStatusPath(string componentUid, string statusUid)
        {
            return Path.Combine(BuildComponentPath(componentUid), "Status", statusUid);
        }

        private List<DayPath> BuildDayPaths(DateTime begin, DateTime end)
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

        private static long GetDirectorySize(string path)
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