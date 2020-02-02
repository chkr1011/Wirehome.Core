using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Wirehome.Core.Diagnostics;

namespace Wirehome.Core.Hardware.GPIO.Adapters
{
    public class LinuxGpioAdapter : IGpioAdapter
    {
        readonly Dictionary<int, InterruptMonitor> _interruptMonitors = new Dictionary<int, InterruptMonitor>();
        readonly object _syncRoot = new object();
        readonly SystemStatusService _systemStatusService;
        readonly ILogger _logger;

        Thread _workerThread;

        public LinuxGpioAdapter(SystemStatusService systemStatusService, ILogger logger)
        {
            _systemStatusService = systemStatusService ?? throw new ArgumentNullException(nameof(systemStatusService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Enable()
        {
            // Do not use tasks because polling must be fast as possible and task library overhead (continuation,
            // cancellation, async/await, thread pool) is not needed.
            _workerThread = new Thread(PollGpios)
            {
                Priority = ThreadPriority.AboveNormal,
                Name = nameof(LinuxGpioAdapter),
                IsBackground = true
            };

            _workerThread.Start();

            _logger.LogInformation("Started GPIO polling thread.");
        }

        public event EventHandler<GpioAdapterStateChangedEventArgs> GpioStateChanged;

        public void SetDirection(int gpioId, GpioDirection direction)
        {
            lock (_syncRoot)
            {
                Export(gpioId);

                var fileContent = direction == GpioDirection.Output ? "out" : "in";
                File.WriteAllText("/sys/class/gpio/gpio" + gpioId + "/direction", fileContent, Encoding.ASCII);
            }
        }

        public void WriteState(int gpioId, GpioState state)
        {
            lock (_syncRoot)
            {
                var fileContent = state == GpioState.Low ? "0" : "1";
                File.WriteAllText("/sys/class/gpio/gpio" + gpioId + "/value", fileContent, Encoding.ASCII);
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
                var fileContent = File.ReadAllText("/sys/class/gpio/gpio" + gpioId + "/value", Encoding.ASCII).Trim();
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

                    File.WriteAllText("/sys/class/gpio/gpio" + gpioId + "/direction", "in", Encoding.ASCII);

                    // TODO: Edge is only required if the state is read via blocked thread.
                    //File.WriteAllText("/sys/class/gpio/gpio" + gpioId + "/edge", edge.ToString().ToLowerInvariant());
                }

                var valuePath = $"/sys/class/gpio/gpio{gpioId}/value";

                var initialState = ReadState(gpioId);
                _interruptMonitors.Add(gpioId, new InterruptMonitor
                {
                    LatestState = initialState,
                    ValuePath = valuePath,
                    ValueFile = File.Open(valuePath, FileMode.Open, FileAccess.Read, FileShare.Read)
                });
            }
        }

        void Export(int gpioId)
        {
            var path = "/sys/class/gpio/gpio" + gpioId;
            if (Directory.Exists(path))
            {
                return;
            }

            File.WriteAllText("/sys/class/gpio/export", gpioId.ToString(), Encoding.ASCII);

            _logger.Log(LogLevel.Debug, $"Exported GPIO {gpioId}.");
        }

        void PollGpios()
        {
            //var threadId = (int)SafeNativeMethods.Syscall(186);
            //_systemStatusService.Set("thread_" + threadId, "gio_monitor");

            var readBuffer = new byte[1];

            while (true)
            {
                try
                {
                    List<GpioAdapterStateChangedEventArgs> eventArgsList;

                    lock (_interruptMonitors)
                    {
                        eventArgsList = new List<GpioAdapterStateChangedEventArgs>(_interruptMonitors.Count);

                        foreach (var interruptMonitor in _interruptMonitors)
                        {
                            interruptMonitor.Value.ValueFile.Seek(0, SeekOrigin.Begin);
                            interruptMonitor.Value.ValueFile.Read(readBuffer, 0, 1);

                            //var fileContent = File.ReadAllText(interruptMonitor.Value.GpioValuePath, Encoding.ASCII).Trim();
                            //var currentState = fileContent == "1" ? GpioState.High : GpioState.Low;

                            var currentState = readBuffer[0] == '1' ? GpioState.High : GpioState.Low;

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
                catch (ThreadAbortException)
                {
                }
                catch (Exception exception)
                {
                    _logger.Log(LogLevel.Error, exception, "Error while polling interrupt inputs.");
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }
        }

        void OnGpioChanged(GpioAdapterStateChangedEventArgs eventArgs)
        {
            _logger.Log(LogLevel.Information, "GPIO {0} changed from {1} to {2}.", eventArgs.GpioId, eventArgs.OldState, eventArgs.NewState);
            GpioStateChanged?.Invoke(this, eventArgs);
        }
    }
}
