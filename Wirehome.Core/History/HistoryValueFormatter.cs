using System;
using System.Globalization;

namespace Wirehome.Core.History
{
    public class HistoryValueFormatter
    {
        public string FormatValue(string value, HistoryValueFormatterOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));

            if (!decimal.TryParse(value, out var parsedValue))
            {
                // The value is no number so that formatting is not possible at all.
                return value;
            }

            if (options.Decimals.HasValue)
            {
                parsedValue = Math.Round(parsedValue, options.Decimals.Value);
            }

            if (!string.IsNullOrEmpty(options.Format))
            {
                return parsedValue.ToString(options.Format, CultureInfo.InvariantCulture);
            }
            else
            {
                return parsedValue.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}