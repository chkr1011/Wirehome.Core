using System;

namespace Wirehome.Core.Repository
{
    public class RepositoryEntityUid
    {
        public string Id { get; set; }

        public string Version { get; set; }

        public RepositoryEntityUid()
        {
        }

        public RepositoryEntityUid(string id, string version)
        {
            Id = id;
            Version = version;
        }

        public static RepositoryEntityUid Parse(string source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var parts = source.Split(new[] { "@" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1 || parts.Length > 2)
            {
                throw new ArgumentException(nameof(source));
            }

            string version = null;
            if (parts.Length == 2)
            {
                version = parts[1];
            }

            return new RepositoryEntityUid
            {
                Id = parts[0],
                Version = version
            };
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Version))
            {
                return Id + "@<latest>";
            }

            return Id + "@" + Version;
        }
    }
}
