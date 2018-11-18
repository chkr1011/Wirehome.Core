#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Proxies;

namespace Wirehome.Core.Diagnostics
{
    public class SystemStatusPythonProxy : IInjectedPythonProxy
    {
        private readonly SystemStatusService _systemInformationService;

        public SystemStatusPythonProxy(SystemStatusService systemInformationService)
        {
            _systemInformationService = systemInformationService ?? throw new ArgumentNullException(nameof(systemInformationService));
        }

        public string ModuleName { get; } = "system_status";

        public void set(string key, object value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            _systemInformationService.Set(key, value);
        }

        public void set(string key, Func<object> valueProvider)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            _systemInformationService.Set(key, valueProvider);
        }

        public object get(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            return _systemInformationService.Get(key);
        }

        public void delete(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            _systemInformationService.Delete(key);
        }
    }
}
