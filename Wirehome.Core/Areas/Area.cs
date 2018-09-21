using System.Collections.Generic;
using Wirehome.Core.Model;

namespace Wirehome.Core.Areas
{
    public class Area
    {
        public Area(string uid)
        {
            Uid = uid;
        }

        public string Uid { get; }

        public WirehomeDictionary Settings { get; } = new WirehomeDictionary();

        public WirehomeDictionary Status { get; } = new WirehomeDictionary();

        public HashSet<string> Components { get; } = new HashSet<string>();

        public HashSet<string> Scenes { get; } = new HashSet<string>();
    }
}
