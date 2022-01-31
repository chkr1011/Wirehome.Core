using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Wirehome.Core.Scheduler
{
    public sealed class ActiveThread : IDisposable
    {
        readonly CancellationTokenSource _ownCancellationTokenSource = new();
        readonly CancellationToken _externalCancellationToken;
        readonly ILogger _logger;
        readonly Action<StartThreadCallbackParameters> _action;
        readonly object _state;

        CancellationTokenSource _cancellationTokenSource;

        public ActiveThread(string uid, Action<StartThreadCallbackParameters> action, object state, ILogger logger, CancellationToken cancellationToken)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _state = state;
            _externalCancellationToken = cancellationToken;

            CreatedTimestamp = DateTime.UtcNow;
        }

        public string Uid { get; }

        public DateTime CreatedTimestamp { get; }

        public int ManagedThreadId { get; private set; }

        public void Stop()
        {
            _ownCancellationTokenSource.Cancel(false);
        }

        public Action StoppedCallback { get; set; }

        public void Dispose()
        {
            _ownCancellationTokenSource?.Dispose();
            _cancellationTokenSource?.Dispose();
        }

        public void Start()
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_ownCancellationTokenSource.Token, _externalCancellationToken);

            Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        ManagedThreadId = Thread.CurrentThread.ManagedThreadId;

                        _logger.LogInformation($"Thread '{Uid}' started.");

                        _action(new StartThreadCallbackParameters(Uid, _state));

                        _logger.LogInformation($"Thread '{Uid}' exited normally.");
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, $"Error while executing thread '{Uid}'.");
                    }
                    finally
                    {
                        _ownCancellationTokenSource.Dispose();
                    }
                },
                _cancellationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }
    }
}