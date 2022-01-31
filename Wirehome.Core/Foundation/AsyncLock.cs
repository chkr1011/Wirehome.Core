using System;
using System.Threading;
using System.Threading.Tasks;

namespace Wirehome.Core.Foundation
{
    public sealed class AsyncLock : IDisposable
    {
        readonly SemaphoreSlim _semaphore = new(1, 1);

        public Task EnterAsync(CancellationToken cancellationToken = default)
        {
            return _semaphore.WaitAsync(cancellationToken);
        }

        public void Enter(CancellationToken cancellationToken = default)
        {
            _semaphore.Wait(cancellationToken);
        }

        public Task EnterAsync(TimeSpan timeout)
        {
            return _semaphore.WaitAsync(timeout);
        }

        public void Enter(TimeSpan timeout)
        {
            _semaphore.Wait(timeout);
        }

        public void Exit()
        {
            _semaphore.Release();
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }
    }
}
