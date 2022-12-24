using Wirehome.Core.Python.Models;

namespace Wirehome.Core.HTTP.Controllers.Models;

public sealed class ActiveTimerModel
{
    public int Interval { get; set; }

    public long InvocationCount { get; set; }

    public long LastDuration { get; set; }

    public ExceptionPythonModel LastException { get; set; }
}