#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;

namespace Wirehome.Core.Python.Proxies
{
    public class DataProviderPythonProxy : IPythonProxy
    {
        public string ModuleName { get; } = "data_provider";

        public string create_guid()
        {
            return Guid.NewGuid().ToString("D");
        }

        public int create_random_number(int min = 0, int max = 100)
        {
            return new Random().Next(min, max);
        }

        public long get_ticks()
        {
            return DateTime.UtcNow.Ticks;
        }
    }
}
