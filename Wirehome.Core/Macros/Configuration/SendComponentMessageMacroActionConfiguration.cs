using Wirehome.Core.Foundation.Model;

namespace Wirehome.Core.Macros.Configuration
{
    public class SendComponentMessageMacroActionConfiguration : MacroActionConfiguration
    {
        public string ComponentUid { get; set; }

        public WirehomeDictionary Message { get; set; }
    }
}
