using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Wirehome.Core.Extensions
{
    public static class ParallelTask
    {
        public static void Start(Func<Task> action, CancellationToken cancellationToken, ILogger logger)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            Task.Factory.StartNew(action, cancellationToken, TaskCreationOptions.None, TaskScheduler.Default).ContinueWith(
                t =>
                {
                    logger.LogWarning(t.Exception, "Error while executing a parallel task.");
                }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}