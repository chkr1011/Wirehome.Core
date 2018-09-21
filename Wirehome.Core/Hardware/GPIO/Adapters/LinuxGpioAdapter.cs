using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Wirehome.Core.Hardware.GPIO.Adapters
{
    public class LinuxGpioAdapter : IGpioAdapter
    {
        private readonly Dictionary<int, InterruptMonitor> _interruptMonitors = new Dictionary<int, InterruptMonitor>();
        private readonly object _syncRoot = new object();
        private readonly ILogger _logger;

        public LinuxGpioAdapter(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            _logger = loggerFactory.CreateLogger<LinuxGpioAdapter>();
        }

        public void Enable()
        {
            Task.Factory.StartNew(PollInterruptInputs, TaskCreationOptions.LongRunning, CancellationToken.None);
        }

        public event EventHandler<GpioAdapterStateChangedEventArgs> GpioStateChanged;

        public void SetDirection(int gpioId, GpioDirection direction)
        {
            lock (_syncRoot)
            {
                Export(gpioId);

                var fileContent = direction == GpioDirection.Output ? "out" : "in";
                File.WriteAllText("/sys/class/gpio/gpio" + gpioId + "/direction", fileContent);
            }
        }

        public void WriteState(int gpioId, GpioState state)
        {
            lock (_syncRoot)
            {
                var fileContent = state == GpioState.Low ? "0" : "1";
                File.WriteAllText("/sys/class/gpio/gpio" + gpioId + "/value", fileContent);
            }
        }

        public GpioState ReadState(int gpioId)
        {
            lock (_interruptMonitors)
            {
                if (_interruptMonitors.TryGetValue(gpioId, out var interruptMonitor))
                {
                    return interruptMonitor.LatestState;
                }
            }

            lock (_syncRoot)
            {
                var fileContent = File.ReadAllText("/sys/class/gpio/gpio" + gpioId + "/value").Trim();
                return fileContent == "1" ? GpioState.High : GpioState.Low;
            }
        }

        public void EnableInterrupt(int gpioId, GpioInterruptEdge edge)
        {
            lock (_interruptMonitors)
            {
                if (_interruptMonitors.ContainsKey(gpioId))
                {
                    return;
                }

                lock (_syncRoot)
                {
                    Export(gpioId);

                    File.WriteAllText("/sys/class/gpio/gpio" + gpioId + "/direction", "in");

                    // TODO: Edge is only required if the state is read via blocked thread.
                    //File.WriteAllText("/sys/class/gpio/gpio" + gpioId + "/edge", edge.ToString().ToLowerInvariant());
                }
                
                var initialState = ReadState(gpioId);
                _interruptMonitors.Add(gpioId, new InterruptMonitor { LatestState = initialState });
            }
        }

        private void Export(int gpioId)
        {
            var path = "/sys/class/gpio/gpio" + gpioId;
            if (Directory.Exists(path))
            {
                return;
            }

            File.WriteAllText("/sys/class/gpio/export", gpioId.ToString());

            _logger.Log(LogLevel.Debug, $"Exported GPIO {gpioId}.");
        }

        private void PollInterruptInputs(object state)
        {
            while (true)
            {
                try
                {
                    var eventArgsList = new List<GpioAdapterStateChangedEventArgs>();

                    lock (_interruptMonitors)
                    {
                        foreach (var interruptMonitor in _interruptMonitors)
                        {
                            var fileContent = File.ReadAllText("/sys/class/gpio/gpio" + interruptMonitor.Key + "/value").Trim();
                            var currentState = fileContent == "1" ? GpioState.High : GpioState.Low;

                            if (currentState == interruptMonitor.Value.LatestState)
                            {
                                continue;
                            }

                            eventArgsList.Add(new GpioAdapterStateChangedEventArgs(interruptMonitor.Key, interruptMonitor.Value.LatestState, currentState));

                            interruptMonitor.Value.LatestState = currentState;
                            interruptMonitor.Value.Timestamp = DateTime.UtcNow;
                        }
                    }

                    foreach (var eventArgs in eventArgsList)
                    {
                        OnGpioChanged(eventArgs);
                    }

                    Thread.Sleep(10);
                }
                catch (Exception exception)
                {
                    _logger.Log(LogLevel.Error, exception, "Unhandled exception while polling interrupt inputs.");
                    Thread.Sleep(5000);
                }
            }
        }

        private void OnGpioChanged(GpioAdapterStateChangedEventArgs eventArgs)
        {
            _logger.Log(LogLevel.Information, "GPIO {0} changed from {1} to {2}.", eventArgs.GpioId, eventArgs.OldState, eventArgs.NewState);
            GpioStateChanged?.Invoke(this, eventArgs);
        }
    }
}
