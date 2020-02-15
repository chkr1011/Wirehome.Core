#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using System.IO;
using System.Threading;
using Wirehome.Core.Python;
using Wirehome.Core.Storage;

namespace Wirehome.Core.History
{
    public class HistoryServicePythonProxy : IInjectedPythonProxy
    {
        readonly StorageService _storageService;
        readonly HistoryService _historyService;

        public HistoryServicePythonProxy(StorageService storageService, HistoryService historyService)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        }

        public string ModuleName { get; } = "history";

        public void publish(string path, object value)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            path = Path.Combine(_storageService.DataPath, path);

            _historyService.Update(new HistoryUpdateOperation()
            {
                Path = path,
                Timestamp = DateTime.UtcNow,
                Value = value
            }, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}