#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using Wirehome.Core.Diagnostics;

namespace Wirehome.Core.Python.Proxies
{
    public class SystemInformationPythonProxy : IPythonProxy
    {
        private readonly SystemStatusService _systemInformationService;

        public SystemInformationPythonProxy(SystemStatusService systemInformationService)
        {
            _systemInformationService = systemInformationService ?? throw new ArgumentNullException(nameof(systemInformationService));
        }

        public string ModuleName { get; } = "system_information";

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
