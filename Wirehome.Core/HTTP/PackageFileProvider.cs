using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Internal;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using Wirehome.Core.GlobalVariables;
using Wirehome.Core.Packages;

namespace Wirehome.Core.HTTP
{
    public class PackageFileProvider : IFileProvider
    {
        readonly HashSet<string> _defaultFileNames = new() { "index.htm", "index.html", "default.htm", "default.html" };
        readonly string _packageUidGlobalVariableUid;
        readonly GlobalVariablesService _globalVariablesService;
        readonly PackageManagerService _packageManagerService;

        public PackageFileProvider(string packageUidGlobalVariableUid, GlobalVariablesService globalVariablesService, PackageManagerService packageManagerService)
        {
            _packageUidGlobalVariableUid = packageUidGlobalVariableUid ?? throw new ArgumentNullException(nameof(packageUidGlobalVariableUid));
            _globalVariablesService = globalVariablesService ?? throw new ArgumentNullException(nameof(globalVariablesService));
            _packageManagerService = packageManagerService ?? throw new ArgumentNullException(nameof(packageManagerService));
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            if (subpath == null) throw new ArgumentNullException(nameof(subpath));

            var fullPath = subpath.Trim(Path.PathSeparator, Path.AltDirectorySeparatorChar);

            if (_defaultFileNames.Contains(fullPath))
            {
                subpath = "index.html";
            }

            var packageUid = _globalVariablesService.GetValue(_packageUidGlobalVariableUid) as string;
            var packageRootPath = _packageManagerService.GetPackageRootPath(PackageUid.Parse(packageUid));

            fullPath = Path.Combine(packageRootPath, fullPath);

            if (!File.Exists(fullPath))
            {
                return new NotFoundFileInfo(subpath);
            }

            return new PhysicalFileInfo(new FileInfo(fullPath));
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            if (subpath == null) throw new ArgumentNullException(nameof(subpath));

            var packageUid = _globalVariablesService.GetValue(_packageUidGlobalVariableUid) as string;
            var packageRootPath = _packageManagerService.GetPackageRootPath(PackageUid.Parse(packageUid));

            var fullPath = Path.Combine(packageRootPath, subpath.Trim(Path.PathSeparator, Path.AltDirectorySeparatorChar));

            if (!Directory.Exists(fullPath))
            {
                return new NotFoundDirectoryContents();
            }

            return new PhysicalDirectoryContents(fullPath);
        }

        public IChangeToken Watch(string filter)
        {
            return null;
        }
    }
}
