#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace Wirehome.Core.Python.Proxies.OS;

public class ExecuteResult
{
    public string ErrorData { get; set; }

    public int ExitCode { get; set; }
    public string OutputData { get; set; }
}