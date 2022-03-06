#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using Wirehome.Core.Python;

namespace Wirehome.Core.Packages;

public sealed class PackageManagerServicePythonProxy : IInjectedPythonProxy
{
    readonly PackageManagerService _packageManagerService;

    public PackageManagerServicePythonProxy(PackageManagerService packageManagerService)
    {
        _packageManagerService = packageManagerService ?? throw new ArgumentNullException(nameof(packageManagerService));
    }

    public string ModuleName { get; } = "package_manager";

    public void download_package(string uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        var packageUid = PackageUid.Parse(uid);
        _packageManagerService.DownloadPackageAsync(packageUid).GetAwaiter().GetResult();
    }

    public string get_file_uri(string uid, string filename)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        var packageUid = PackageUid.Parse(uid);
        return $"/packages/{packageUid.Id}/{packageUid.Version}/{filename}";
    }
}