using System;
using Wirehome.Core.Exceptions;

namespace Wirehome.Core.Packages.Exceptions;

public sealed class WirehomePackageException : WirehomeException
{
    public WirehomePackageException(string message) : base(message)
    {
    }

    public WirehomePackageException(string message, Exception innerException) : base(message, innerException)
    {
    }
}