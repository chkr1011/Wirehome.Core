using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.History.Extract;
using Wirehome.Core.History.Repository;
using Wirehome.Core.Storage;

namespace Wirehome.Core.History
{
    public partial class HistoryService : IService
    {
        readonly HistoryValueFormatter _valueFormatter = new HistoryValueFormatter();

        readonly StorageService _storageService;
        readonly ILogger _logger;
        readonly HistoryRepository _historyRepository;
        readonly OperationsPerSecondCounter _updateRateCounter;

        HistoryServiceOptions _options;

        long _lastUpdateDuration;
        long _maxUpdateDuration;
        long? _minUpdateDuration;
        long _updatesCount;
        long _totalUpdateDuration;

        public HistoryService(
            StorageService storageService,
            SystemStatusService systemStatusService,
            DiagnosticsService diagnosticsService,
            ILogger<HistoryService> logger)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _historyRepository = new HistoryRepository();

            if (diagnosticsService == null) throw new ArgumentNullException(nameof(diagnosticsService));
            _updateRateCounter = diagnosticsService.CreateOperationsPerSecondCounter("history.update_rate");

            if (systemStatusService == null) throw new ArgumentNullException(nameof(systemStatusService));
            systemStatusService.Set("history.update_rate", () => _updateRateCounter.Count);
            systemStatusService.Set("history.updates_count", () => _updatesCount);
            systemStatusService.Set("history.last_update_duration", () => _lastUpdateDuration);
            systemStatusService.Set("history.max_update_duration", () => _maxUpdateDuration);
            systemStatusService.Set("history.min_update_duration", () => _minUpdateDuration);
            systemStatusService.Set("history.average_update_duration", () =>
            {
                if (_updatesCount == 0)
                {
                    return double.NaN;
                }

                return _totalUpdateDuration / (decimal)_updatesCount;
            });
        }

        public void Start()
        {
            _storageService.TryReadOrCreate(out _options, DefaultDirectoryNames.Configuration, HistoryServiceOptions.Filename);

            if (!_options.IsEnabled)
            {
                _logger.LogInformation("History is disabled.");
                return;
            }
        }

        public Task<List<HistoryValueElement>> Read(HistoryReadOperation readOperation, CancellationToken cancellationToken)
        {
            return _historyRepository.Read(readOperation, cancellationToken);
        }

        public async Task Update(HistoryUpdateOperation updateOperation, CancellationToken cancellationToken)
        {
            if (updateOperation is null) throw new ArgumentNullException(nameof(updateOperation));

            if (!_options.IsEnabled)
            {
                _logger.LogTrace("Skipping history update because history is disabled.");
                return;
            }

            try
            {
                var stopwatch = Stopwatch.StartNew();

                var serializedValue = Convert.ToString(updateOperation.Value, CultureInfo.InvariantCulture);

                serializedValue = _valueFormatter.FormatValue(serializedValue, updateOperation.ValueFormatterOptions);

                var repositoryUpdateOperation = new HistoryRepositoryUpdateOperation
                {
                    Path = updateOperation.Path,
                    Timestamp = updateOperation.Timestamp,
                    Value = serializedValue,
                    OldValueTimeToLive = updateOperation.OldValueTimeToLive
                };

                await _historyRepository.Write(repositoryUpdateOperation, cancellationToken).ConfigureAwait(false);

                stopwatch.Stop();

                var duration = stopwatch.ElapsedMilliseconds;

                _totalUpdateDuration += duration;
                _updatesCount++;

                if (duration > _maxUpdateDuration)
                {
                    _maxUpdateDuration = duration;
                }

                if (!_minUpdateDuration.HasValue || duration < _minUpdateDuration)
                {
                    _minUpdateDuration = duration;
                }

                _lastUpdateDuration = duration;

                _updateRateCounter.Increment();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, exception, $"Error while updating history ({updateOperation.Path}).");
            }
        }

        public Task<HistoryExtract> BuildHistoryExtractAsync(string path, DateTime rangeStart, DateTime rangeEnd, TimeSpan? interval, HistoryExtractDataType dataType, int maxRowCount, CancellationToken cancellationToken)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            if (_historyRepository == null)
            {
                return null;
            }

            return new HistoryExtractBuilder(_historyRepository).BuildAsync(path, rangeStart, rangeEnd, interval, dataType, maxRowCount, cancellationToken);
        }

        public void ResetStatistics()
        {
            _maxUpdateDuration = 0L;
            _minUpdateDuration = null;
            _updatesCount = 0L;
            _totalUpdateDuration = 0L;
        }

        public Task DeleteHistory(string path, CancellationToken cancellationToken)
        {
            return _historyRepository.DeleteHistory(path, cancellationToken);
        }

        public Task<long> GetHistorySize(string path, CancellationToken cancellationToken)
        {
            return _historyRepository.GetHistorySize(path, cancellationToken);
        }
    }
}