using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Wirehome.Core.Storage;

public sealed class StoragePaths
{
    public StoragePaths()
    {
        BinPath = AppDomain.CurrentDomain.BaseDirectory;

        var customDataPath = Path.Combine(BinPath, "etc", "wirehome");
        if (Directory.Exists(customDataPath))
        {
            DataPath = customDataPath;
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            DataPath = Path.Combine("/etc/wirehome/");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            DataPath = Path.Combine(Environment.ExpandEnvironmentVariables("%appData%"), "Wirehome");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            DataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".wirehome");
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public string BinPath { get; }

    public string DataPath { get; }
}