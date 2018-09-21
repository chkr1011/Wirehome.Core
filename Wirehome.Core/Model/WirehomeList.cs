using System.Collections.Generic;
using IronPython.Runtime;

namespace Wirehome.Core.Model
{
    public class WirehomeList : List<object>
    {
        public static implicit operator WirehomeList(List list)
        {
            if (list == null)
            {
                return null;
            }

            // TODO: Complete.
            return new WirehomeList();
        }

        public WirehomeList WithValue(object value)
        {
            Add(value);
            return this;
        }
    }
}
