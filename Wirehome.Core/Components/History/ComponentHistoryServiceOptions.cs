using System;
using System.Collections.Generic;
using Wirehome.Core.History.Repository;

namespace Wirehome.Core.Components.History;

public sealed class ComponentHistoryServiceOptions
{
    public const string Filename = "ComponentHistoryServiceConfiguration.json";

    public HashSet<string> ComponentBlacklist { get; set; } = new();

    public HashSet<string> ComponentStatusBlacklist { get; set; } = new();

    public Dictionary<string, Dictionary<string, object>> ComponentStatusDefaultSettings { get; set; } = new();

    public TimeSpan ComponentStatusPullInterval { get; set; } = TimeSpan.FromMinutes(5);

    public bool IsEnabled { get; set; } = true;

    public HashSet<string> StatusBlacklist { get; set; } = new();

    public Dictionary<string, Dictionary<string, object>> StatusDefaultSettings { get; set; } = new()
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
}