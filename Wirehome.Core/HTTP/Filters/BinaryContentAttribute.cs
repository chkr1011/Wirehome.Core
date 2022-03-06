using System;

namespace Wirehome.Core.HTTP.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class BinaryContentAttribute : Attribute
    {
    }
}
