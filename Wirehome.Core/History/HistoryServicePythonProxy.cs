#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using System.Globalization;
using System.Threading;
using Wirehome.Core.History.Repository;
using Wirehome.Core.Python;

namespace Wirehome.Core.History
{
    public class HistoryServicePythonProxy : IInjectedPythonProxy
    {
        private readonly HistoryService _historyService;

        public HistoryServicePythonProxy(HistoryService historyService)
        {
            _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        }

        public string ModuleName { get; } = "history";

        public void publish(string uid, object value)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _historyService.Publish(new HistoryUpdate()
            {
                Path = uid,
                Timestamp = DateTime.UtcNow,
                Value = Convert.ToString(value, CultureInfo.InvariantCulture)
            }, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}