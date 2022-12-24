using System.Collections.Generic;

namespace Wirehome.Core.Macros.Configuration;

public class SendComponentMessageMacroActionConfiguration : MacroActionConfiguration
{
    public string ComponentUid { get; set; }

    public IDictionary<object, object> Message { get; set; }
}