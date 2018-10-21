using System;
using System.Collections.Generic;

namespace Wirehome.Core.History
{
    public class HistoryServiceOptions
    {
        public bool IsEnabled { get; set; } = true;

        public TimeSpan ComponentStatusPullInterval { get; set; } = TimeSpan.FromMinutes(5);

        public HashSet<string> ComponentBlacklist { get; set; } = new HashSet<string>();

        public HashSet<string> ComponentStatusBlacklist { get; set; } = new HashSet<string>();

        public HashSet<string> ComponentWithStatusBlacklist { get; set; } = new HashSet<string>();
    }
}