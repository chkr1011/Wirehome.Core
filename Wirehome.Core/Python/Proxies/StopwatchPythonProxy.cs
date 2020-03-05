#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Wirehome.Core.Python.Proxies
{
    public class StopwatchPythonProxy : IInjectedPythonProxy
    {
        private readonly Dictionary<string, Stopwatch> _stopwatches = new Dictionary<string, Stopwatch>();

        public string ModuleName { get; } = "stopwatch";

        public string start(string uid)
        {
            if (string.IsNullOrEmpty(uid))
            {
                uid = Guid.NewGuid().ToString("D");
            }

            lock (_stopwatches)
            {
                if (!_stopwatches.TryGetValue(uid, out var stopwatch))
                {
                    stopwatch = Stopwatch.StartNew();
                    _stopwatches.Add(uid, stopwatch);
                }
                else
                {
                    stopwatch.Start();
                }
            }

            return uid;
        }

        public string restart(string uid)
        {
            if (string.IsNullOrEmpty(uid))
            {
                uid = Guid.NewGuid().ToString("D");
            }

            lock (_stopwatches)
            {
                _stopwatches[uid] = Stopwatch.StartNew();
            }

            return uid;
        }

        public void stop(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_stopwatches)
            {
                _stopwatches.Remove(uid);
            }
        }

        public object get_elapsed_millis(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_stopwatches)
            {
                if (!_stopwatches.TryGetValue(uid, out var stopwatch))
                {
                    return null;
                }

                return stopwatch.ElapsedMilliseconds;
            }
        }

        public object get_elapsed_seconds(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_stopwatches)
            {
                if (!_stopwatches.TryGetValue(uid, out var stopwatch))
                {
                    return null;
                }

                return stopwatch.Elapsed.Seconds;
            }
        }

        public object get_elapsed_minutes(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_stopwatches)
            {
                if (!_stopwatches.TryGetValue(uid, out var stopwatch))
                {
                    return null;
                }

                return stopwatch.Elapsed.Minutes;
            }
        }

        public object get_elapsed_hours(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_stopwatches)
            {
                if (!_stopwatches.TryGetValue(uid, out var stopwatch))
                {
                    return null;
                }

                return stopwatch.Elapsed.Hours;
            }
        }
    }
}
