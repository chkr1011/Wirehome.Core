using System;
using System.Globalization;

#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace Wirehome.Core.Python.Proxies
{
    public class DateTimePythonProxy : IPythonProxy
    {
        public string ModuleName { get; } = "date_time";

        public string now()
        {
            return DateTime.Now.ToString("O", CultureInfo.InvariantCulture);
        }

        //public bool is_after_sunrise(string shift = null)
        //{
        //    return CheckCondition("outdoor.sunrise", ">", shift);
        //}

        //public bool is_after_sunset(string shift = null)
        //{
        //    return CheckCondition("outdoor.sunset", ">", shift);
        //}

        //private bool CheckCondition(string globalVariableUid, string operation, string shift)
        //{
        //    if (!_globalVariablesService.TryGetValue<string>(globalVariableUid, null, out var valueSource))
        //    {
        //        return false;
        //    }

        //    if (!TimeSpan.TryParse(valueSource, CultureInfo.InvariantCulture, out var value))
        //    {
        //        return false;
        //    }

        //    if (!string.IsNullOrEmpty(shift))
        //    {
        //        value += TimeSpan.Parse(shift, CultureInfo.InvariantCulture);
        //    }

        //    var now = DateTime.Now.TimeOfDay;

        //    if (operation == ">")
        //    {
        //        return now > value;
        //    }

        //    if (operation == "<")
        //    {
        //        return now < value;
        //    }

        //    if (operation == "=")
        //    {
        //        return now == value;
        //    }

        //    return false;
        //}
    }
}
