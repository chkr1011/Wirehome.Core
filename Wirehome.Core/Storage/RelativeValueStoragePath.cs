using System;
using System.Collections.Generic;

namespace Wirehome.Core.Storage;

public sealed class RelativeValueStoragePath
{
    public RelativeValueStoragePath()
    {
    }

    public RelativeValueStoragePath(string segment)
    {
        if (segment is null)
        {
            throw new ArgumentNullException(nameof(segment));
        }

        if (segment.Contains('/', StringComparison.Ordinal))
        {
            throw new ArgumentException("The segment contains invalid chars (/).");
        }

        Segments.Add(segment);
    }

    public RelativeValueStoragePath(params string[] segments)
    {
        if (segments is null)
        {
            throw new ArgumentNullException(nameof(segments));
        }

        foreach (var segment in segments)
        {
            if (segment.Contains('/', StringComparison.Ordinal))
            {
                throw new ArgumentException("The segment contains invalid chars (/).");
            }

            Segments.Add(segment);
        }
    }

    public List<string> Segments { get; } = new();

    public static RelativeValueStoragePath Parse(string path)
    {
        if (path == null)
        {
            return null;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return new RelativeValueStoragePath(segments);
    }

    public override string ToString()
    {
        return string.Join("/", Segments);
    }
}