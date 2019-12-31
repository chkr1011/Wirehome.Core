using System;
using System.Collections.Generic;
using System.Globalization;
using Wirehome.Core.History.Repository;

namespace Wirehome.Core.History
{
    public class HistoryValueFormatterOptionsFactory
    {
        public HistoryValueFormatterOptions Create(IDictionary<string, object> componentSettings, IDictionary<string, object> defaultSettings)
        {
            var options = new HistoryValueFormatterOptions();

            if (TryGetSetting(HistorySettingName.RoundDigits, componentSettings, defaultSettings, out var roundDigitsValue))
            {
                options.Decimals = Convert.ToInt32(roundDigitsValue, CultureInfo.InvariantCulture);
            }

            if (TryGetSetting(HistorySettingName.Format, componentSettings, defaultSettings, out var formatValue))
            {
                options.Format = Convert.ToString(formatValue);
            }

            return options;
        }

        bool TryGetSetting(string settingUid, IDictionary<string, object> componentSettings, IDictionary<string, object> defaultSettings, out object value)
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
}