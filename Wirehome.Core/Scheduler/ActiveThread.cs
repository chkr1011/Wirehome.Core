using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Wirehome.Core.Scheduler
{
    public sealed class ActiveThread : IDisposable
    {
        private readonly CancellationTokenSource _ownCancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _externalCancellationToken;
        private readonly ILogger _logger;
        private readonly Action<StartThreadCallbackParameters> _action;
        private readonly object _state;

        public ActiveThread(string uid, Action<StartThreadCallbackParameters> action, object state, CancellationToken cancellationToken, ILogger logger)
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
        }

        public void Start()
        {
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _ownCancellationTokenSource.Token, 
                _externalCancellationToken);

            Task.Factory.StartNew(() =>
            {
                try
                {
                    Thread.CurrentThread.Name = Uid;
                    ManagedThreadId = Thread.CurrentThread.ManagedThreadId;

                    _action(new StartThreadCallbackParameters(Uid, _state));

                    _logger.Log(LogLevel.Information, $"Thread '{Uid}' exited normally.");
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    _logger.Log(LogLevel.Error, exception, $"Error while executing thread '{Uid}'.");
                }
                finally
                {
                    _ownCancellationTokenSource.Dispose();
                }
            }, linkedCts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
    }
}