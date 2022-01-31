using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Wirehome.Core.Macros.Configuration
{
    public class MacroConfiguration
    {
        public bool IsEnabled { get; set; } = true;

        public List<JObject> Actions { get; set; } = new();
    }
}
