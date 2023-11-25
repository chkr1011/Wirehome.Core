using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Wirehome.Core.MessageBus;

public sealed class MessageBusSubscriber
{
    readonly Action<IDictionary<object, object>> _callback;
    readonly ILogger _logger;

    long _faultedMessagesCount;
    long _processedMessagesCount;

    public MessageBusSubscriber(string uid, IDictionary<object, object> filter, Action<IDictionary<object, object>> callback, ILogger logger)
    {
        if (filter == null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        Uid = uid ?? throw new ArgumentNullException(nameof(uid));
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Pre convert all items to increase performance.
        Filter = new Dictionary<string, string>();
        foreach (var filterItem in filter)
        {
            var key = Convert.ToString(filterItem.Key, CultureInfo.InvariantCulture) ?? string.Empty;
            Filter[key] = Convert.ToString(filterItem.Value, CultureInfo.InvariantCulture) ?? string.Empty;
        }
    }

    public long FaultedMessagesCount => Interlocked.Read(ref _faultedMessagesCount);

    public Dictionary<string, string> Filter { get; }

    public long ProcessedMessagesCount => Interlocked.Read(ref _processedMessagesCount);

    public string Uid { get; }

    public void ProcessMessage(IDictionary<object, object> message)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        try
        {
            _callback.Invoke(message);

            Interlocked.Increment(ref _processedMessagesCount);
        }
        catch (Exception exception)
        {
            if (exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "Error while processing bus message for subscriber '{0}'", Uid);
            }

            Interlocked.Increment(ref _faultedMessagesCount);
        }
    }
}