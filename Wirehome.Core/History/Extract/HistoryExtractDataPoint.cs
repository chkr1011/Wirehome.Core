using System;
using System.Diagnostics;
using System.Globalization;

namespace Wirehome.Core.History.Extract
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class HistoryExtractDataPoint
    {
        public DateTime Timestamp { get; set; }

        public object Value { get; set; }

        private string DebuggerDisplay
        {
            get
            {
                string valueString;
                if (Value == null)
                {
                    valueString = "<null>";
                }
                else
                {
                    valueString = Convert.ToString(Value, CultureInfo.InvariantCulture);
                }

                return Timestamp.ToString("O") + "=" + valueString;
            }
        }
    }
}
