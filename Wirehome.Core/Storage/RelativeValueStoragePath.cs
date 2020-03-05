using System;
using System.Collections.Generic;

namespace Wirehome.Core.Storage
{
    public class RelativeValueStoragePath
    {
        public RelativeValueStoragePath()
        {
        }

        public RelativeValueStoragePath(string path)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));

            Paths.Add(path);
        }

        public RelativeValueStoragePath(params string[] paths)
        {
            if (paths is null) throw new ArgumentNullException(nameof(paths));

            Paths.AddRange(paths);
        }

        public List<string> Paths { get; } = new List<string>();

        public override string ToString()
        {
            return string.Join("/", Paths);
        }

        public static RelativeValueStoragePath Parse(string path)
        {
            if (path == null)
            {
                return null;
            }

            var paths = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return new RelativeValueStoragePath(paths);
        }
    }

}
