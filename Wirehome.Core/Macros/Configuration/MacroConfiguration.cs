using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Wirehome.Core.Macros.Configuration
{
    public class MacroConfiguration
    {
        public bool IsEnabled { get; set; } = true;

        public List<JObject> Actions { get; set; } = new List<JObject>();
    }
}
