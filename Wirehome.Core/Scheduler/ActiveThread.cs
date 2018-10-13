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
        private readonly Action<string> _action;

        public ActiveThread(string uid, Action<string> action, CancellationToken cancellationToken, ILogger logger)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _externalCancellationToken = cancellationToken;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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

                    _action(Uid);

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