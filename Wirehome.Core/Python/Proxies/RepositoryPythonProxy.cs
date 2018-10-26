#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using Wirehome.Core.Repository;

namespace Wirehome.Core.Python.Proxies
{
    public class RepositoryPythonProxy : IPythonProxy
    {
        public string ModuleName { get; } = "repository";

        public string get_file_uri(string uid, string filename)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var entityUid = RepositoryEntityUid.Parse(uid);
            return $"/repository/{entityUid.Id}/{entityUid.Version}/{filename}";
        }
    }
}
