using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Wirehome.Core.Extensions;

public static class ParallelTask
{
    public static void Start(Func<Task> action, CancellationToken cancellationToken, ILogger logger, TaskCreationOptions creationOptions = TaskCreationOptions.None)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        Task.Factory.StartNew(action, cancellationToken, creationOptions, TaskScheduler.Default)
            .ContinueWith(t => { logger.LogWarning(t.Exception, "Error while executing a parallel task."); }, TaskContinuationOptions.OnlyOnFaulted);
    }

    public static void Start(Action action, CancellationToken cancellationToken, ILogger logger, TaskCreationOptions creationOptions = TaskCreationOptions.None)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        Task.Factory.StartNew(action, cancellationToken, creationOptions, TaskScheduler.Default)
            .ContinueWith(t => { logger.LogWarning(t.Exception, "Error while executing a parallel task."); }, TaskContinuationOptions.OnlyOnFaulted);
    }
}