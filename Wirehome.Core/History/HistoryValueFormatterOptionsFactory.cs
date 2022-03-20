using System;
using System.Collections.Generic;
using System.Globalization;
using Wirehome.Core.History.Repository;

namespace Wirehome.Core.History;

public static class HistoryValueFormatterOptionsFactory
{
    public static HistoryValueFormatterOptions Create(IReadOnlyDictionary<string, object> componentSettings, IDictionary<string, object> defaultSettings)
    {
        var options = new HistoryValueFormatterOptions();

        if (TryGetSetting(HistorySettingName.Decimals, componentSettings, defaultSettings, out var roundDigitsValue))
        {
            options.Decimals = Convert.ToInt32(roundDigitsValue, CultureInfo.InvariantCulture);
        }

        if (TryGetSetting(HistorySettingName.Format, componentSettings, defaultSettings, out var formatValue))
        {
            options.Format = Convert.ToString(formatValue);
        }

        return options;
    }

    static bool TryGetSetting(string settingUid, IReadOnlyDictionary<string, object> componentSettings, IDictionary<string, object> defaultSettings, out object value)
    {
        if (componentSettings != null)
        {
            if (componentSettings.TryGetValue(settingUid, out value))
            {
                return true;
            }
        }

        if (defaultSettings != null)
        {
            if (defaultSettings.TryGetValue(settingUid, out value))
            {
                return true;
            }
        }

        value = null;
        return false;
    }
}