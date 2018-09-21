using System.Collections.Generic;
using Wirehome.Core.Model;

namespace Wirehome.Core.Macros
{
    public class Macro
    {
        public List<WirehomeDictionary> Commands { get; } = new List<WirehomeDictionary>();

        public string Script { get; set; }
    }
}