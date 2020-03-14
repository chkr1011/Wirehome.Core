using System;
using System.Threading;

namespace Wirehome.Core.System
{
    public sealed class SystemCancellationToken : IDisposable
    {
        readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public CancellationToken Token => _cancellationTokenSource.Token;

        public void Cancel()
        {
            _cancellationTokenSource.Cancel(false);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
        }
    }
}
