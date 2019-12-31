#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Wirehome.Core.History.Repository;
using Wirehome.Core.Python;
using Wirehome.Core.Storage;

namespace Wirehome.Core.History
{
    public class HistoryServicePythonProxy : IInjectedPythonProxy
    {
        private readonly StorageService _storageService;
        private readonly HistoryService _historyService;

        public HistoryServicePythonProxy(StorageService storageService, HistoryService historyService)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        }

        public string ModuleName { get; } = "history";

        public void publish(string uid, object value)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var path = Path.Combine(_storageService.DataPath, uid);

            _historyService.Update(new HistoryUpdateOperation()
            {
                Path = path,
                Timestamp = DateTime.UtcNow,
                Value = Convert.ToString(value, CultureInfo.InvariantCulture)
            }, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}