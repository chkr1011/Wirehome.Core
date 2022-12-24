using System;
using Wirehome.Core.Exceptions;

namespace Wirehome.Core.Python.Exceptions;

public class PythonProxyException : WirehomeException
{
    public PythonProxyException(string message, Exception exception) : base(message, exception)
    {
    }

    public PythonProxyException(string message) : base(message)
    {
    }
}