#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Wirehome.Core.Python.Proxies
{
    public class ConverterPythonProxy : IPythonProxy
    {
        public string ModuleName { get; } = "convert";

        public string file_time_to_local_date_time(int seconds)
        {
            var buffer = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return buffer.AddSeconds(seconds).ToLocalTime().ToString("O");
        }

        public string file_time_to_local_time(int seconds)
        {
            var buffer = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return buffer.AddSeconds(seconds).ToLocalTime().TimeOfDay.ToString("c");
        }

        public string file_time_to_utc_date_time(int seconds)
        {
            var buffer = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return buffer.AddSeconds(seconds).ToUniversalTime().ToString("O");
        }

        public string file_time_to_utc_time(int seconds)
        {
            var buffer = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return buffer.AddSeconds(seconds).ToUniversalTime().TimeOfDay.ToString("c");
        }

        public int day_from_date(string date)
        {
            return DateTime.ParseExact(date, "O", CultureInfo.InvariantCulture).Day;
        }

        public int month_from_date(string date)
        {
            return DateTime.ParseExact(date, "O", CultureInfo.InvariantCulture).Month;
        }

        public int year_from_date(string date)
        {
            return DateTime.ParseExact(date, "O", CultureInfo.InvariantCulture).Year;
        }

        public int hour_from_date(string date)
        {
            return DateTime.ParseExact(date, "O", CultureInfo.InvariantCulture).Hour;
        }

        public int minute_from_date(string date)
        {
            return DateTime.ParseExact(date, "O", CultureInfo.InvariantCulture).Minute;
        }

        public int second_from_date(string date)
        {
            return DateTime.ParseExact(date, "O", CultureInfo.InvariantCulture).Second;
        }

        public string to_string(object data)
        {
            if (data == null)
            {
                return null;
            }

            if (data is string s)
            {
                return s;
            }

            if (data is IEnumerable<object> o)
            {
                return Encoding.UTF8.GetString(o.Select(Convert.ToByte).ToArray());
            }

            return string.Empty;
        }

        public object deserialize_json(object source)
        {
            var jsonText = to_string(source);
            var json = JToken.Parse(jsonText);
            return PythonConvert.ConvertForPython(json);
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles