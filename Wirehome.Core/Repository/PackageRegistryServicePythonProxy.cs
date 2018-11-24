#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using Wirehome.Core.Python;

namespace Wirehome.Core.Repository
{
    public class PackageRegistryServicePythonProxy : IInjectedPythonProxy
    {
        private readonly PackageRegistryService _packageRegistryService;

        public PackageRegistryServicePythonProxy(PackageRegistryService packageRegistryService)
        {
            _packageRegistryService = packageRegistryService ?? throw new ArgumentNullException(nameof(packageRegistryService));
        }

        public string ModuleName { get; } = "package_registry";

        public string get_file_uri(string uid, string filename)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var packageUid = PackageUid.Parse(uid);
            return $"/packages/{packageUid.Id}/{packageUid.Version}/{filename}";
        }

        public void download_package(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var packageUid = PackageUid.Parse(uid);
            _packageRegistryService.DownloadPackageAsync(packageUid).GetAwaiter().GetResult();
        }
    }
}
