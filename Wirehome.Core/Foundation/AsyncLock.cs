using System;
using System.Threading;
using System.Threading.Tasks;

namespace Wirehome.Core.Foundation
{
    public class AsyncLock
    {
        readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public Task EnterAsync(CancellationToken cancellationToken = default)
        {
            return _semaphore.WaitAsync(cancellationToken);
        }

        public Task EnterAsync(TimeSpan timeout)
        {
            return _semaphore.WaitAsync(timeout);
        }

        public void Exit()
        {
            _semaphore.Release();
        }
    }
}
