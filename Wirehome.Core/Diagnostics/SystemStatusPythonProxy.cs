#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using Wirehome.Core.Python;

namespace Wirehome.Core.Diagnostics
{
    public class SystemStatusPythonProxy : IInjectedPythonProxy
    {
        readonly SystemStatusService _systemStatusService;

        public SystemStatusPythonProxy(SystemStatusService systemInformationService)
        {
            _systemStatusService = systemInformationService ?? throw new ArgumentNullException(nameof(systemInformationService));
        }

        public delegate object ValueProvider();

        public string ModuleName { get; } = "system_status";

        public void set(string key, object value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            _systemStatusService.Set(key, value);
        }

        public void set(string key, ValueProvider valueProvider)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            _systemStatusService.Set(key, valueProvider);
        }

        public object get(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            return _systemStatusService.Get(key);
        }

        public void delete(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            _systemStatusService.Delete(key);
        }
    }
}
