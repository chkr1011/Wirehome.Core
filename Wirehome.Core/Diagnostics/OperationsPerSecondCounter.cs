using System.Threading;
using System.Threading.Tasks;

namespace Wirehome.Core.Diagnostics
{
    public class OperationsPerSecondCounter
    {
        private int _current;

        public OperationsPerSecondCounter()
        {
            Task.Run(MeasureAsync);
        }

        public int Count { get; private set; }

        public void Increment()
        {
            Interlocked.Increment(ref _current);
        }
        
        private async Task MeasureAsync()
        {
            await Task.Delay(1000).ConfigureAwait(false);
            var current = Interlocked.Exchange(ref _current, 0);

            Count = current;
        }
    }
}
