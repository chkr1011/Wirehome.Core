using System.Threading;

namespace Wirehome.Core.System
{
    public sealed class SystemCancellationToken
    {
        readonly CancellationTokenSource _cancellationTokenSource = new();

        public SystemCancellationToken()
        {
            Token = _cancellationTokenSource.Token;
        }

        public CancellationToken Token { get; }

        public void Cancel()
        {
            _cancellationTokenSource.Cancel(false);
        }
    }
}
