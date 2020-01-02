using System;
using System.Collections.Generic;
using Wirehome.Core.History.Repository;

namespace Wirehome.Core.Components.History
{
    public class ComponentHistoryServiceOptions
    {
        public const string Filename = "ComponentHistoryServiceConfiguration.json";

        public bool IsEnabled { get; set; } = true;

        public TimeSpan ComponentStatusPullInterval { get; set; } = TimeSpan.FromMinutes(5);

        public HashSet<string> ComponentBlacklist { get; set; } = new HashSet<string>();

        public HashSet<string> StatusBlacklist { get; set; } = new HashSet<string>();

        public HashSet<string> ComponentStatusBlacklist { get; set; } = new HashSet<string>();
        
        public Dictionary<string, Dictionary<string, object>> StatusDefaultSettings { get; set; } = new Dictionary<string, Dictionary<string, object>>
        {
            ["temperature.value"] = new Dictionary<string, object>
            {
                [HistorySettingName.Decimals] = 1
            },
            ["humidity.value"] = new Dictionary<string, object>
            {
                [HistorySettingName.Decimals] = 0,
                [HistorySettingName.UpdateOnValueChange] = false
            }
        };

        public Dictionary<string, Dictionary<string, object>> ComponentStatusDefaultSettings { get; set; } = new Dictionary<string, Dictionary<string, object>>
        {
        };
    }
}
