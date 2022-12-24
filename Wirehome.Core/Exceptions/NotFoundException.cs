using System;

namespace Wirehome.Core.Exceptions;

public class NotFoundException : WirehomeException
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}