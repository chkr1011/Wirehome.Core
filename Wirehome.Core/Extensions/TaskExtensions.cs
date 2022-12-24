using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Wirehome.Core.Extensions;

public static class TaskExtensions
{
    public static Task Forget(this Task task, ILogger logger)
    {
        if (task == null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        task.ContinueWith(t => { logger.LogWarning(t.Exception, "A task exception was not observed."); }, TaskContinuationOptions.OnlyOnFaulted);

        return task;
    }
}