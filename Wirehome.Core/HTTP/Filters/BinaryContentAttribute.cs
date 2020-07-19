using System;

namespace Wirehome.Core.HTTP.Filters
{
    /// <summary>
    /// API method attribute to add body/content to Swagger UI using <see cref="BinaryContentFilter" />.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class BinaryContentAttribute : Attribute
    {
    }
}
