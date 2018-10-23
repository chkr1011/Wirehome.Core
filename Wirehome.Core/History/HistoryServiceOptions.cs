using System;
using System.Collections.Generic;
using Wirehome.Core.History.Repository;

namespace Wirehome.Core.History
{
    public class HistoryServiceOptions
    {
        public bool IsEnabled { get; set; } = true;

        public TimeSpan ComponentStatusPullInterval { get; set; } = TimeSpan.FromMinutes(5);

        public HashSet<string> ComponentBlacklist { get; set; } = new HashSet<string>();

        public HashSet<string> ComponentStatusBlacklist { get; set; } = new HashSet<string>();

        public HashSet<string> FullComponentStatusBlacklist { get; set; } = new HashSet<string>();

        public Dictionary<string, Dictionary<string, object>> ComponentStatusDefaultSettings { get; set; } = new Dictionary<string, Dictionary<string, object>>
        {
            ["temperature.value"] = new Dictionary<string, object>
            {
                [HistorySettingName.RoundDigits] = 0
            }
        };
    }
}