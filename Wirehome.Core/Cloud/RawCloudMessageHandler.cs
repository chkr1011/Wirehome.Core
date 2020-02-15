using System;
using System.Collections.Generic;

namespace Wirehome.Core.Cloud
{
    public class RawCloudMessageHandler
    {
        readonly Func<IDictionary<object, object>, IDictionary<object, object>> _callback;

        public RawCloudMessageHandler(string type, Func<IDictionary<object, object>, IDictionary<object, object>> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            TargetType = type ?? throw new ArgumentNullException(nameof(type));
        }

        public string TargetType { get; }

        public IDictionary<object, object> Invoke(IDictionary<object, object> message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            return _callback(message);
        }
    }
}