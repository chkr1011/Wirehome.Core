#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using System.Globalization;

namespace Wirehome.Core.Python.Proxies
{
    public class DateTimeParserPythonProxy : IPythonProxy
    {
        public string ModuleName { get; } = "date_time_parser";

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

        // TODO: Move to "DateTimeParserPythonProxy (date_time_parser.get_day(date)).
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
    }
}
