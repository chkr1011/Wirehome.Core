using System;
using System.Collections.Generic;

namespace Wirehome.Core.System
{
    public class SystemLaunchArguments
    {
        public SystemLaunchArguments(IEnumerable<string> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));

            Values = new List<string>(values);
        }

        public IList<string> Values { get; }
    }
}
