#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using Wirehome.Core.Foundation.Model;

namespace Wirehome.Core.Python.Proxies.OS
{
    public class ExecuteResult : TypedWirehomeDictionary
    {
        public string OutputData { get; set; }

        public string ErrorData { get; set; }

        public int ExitCode { get; set; }
    }
}
