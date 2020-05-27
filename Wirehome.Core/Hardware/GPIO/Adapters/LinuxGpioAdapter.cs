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
                ExportGpio(gpioId);

                var fileContent = direction == GpioDirection.Output ? "out" : "in";
                WriteGpioDirection(gpioId, fileContent);

                _logger.Log(LogLevel.Information, $"Exported GPIO {gpioId}.");
            }
        }

        public void WriteState(int gpioId, GpioState state)
        {
            lock (_syncRoot)
            {
                WrtieGpioState(gpioId, state);
            }
        }

        public GpioState ReadState(int gpioId)
        {
            lock (_syncRoot)
            {
                return ReadGpioState(gpioId);
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
                    ExportGpio(gpioId);
                    WriteGpioDirection(gpioId, "in");

                    _logger.Log(LogLevel.Information, $"Exported GPIO {gpioId}.");

                    // TODO: Edge is only required if the state is read via blocked thread.
                    //File.WriteAllText("/sys/class/gpio/gpio" + gpioId + "/edge", edge.ToString().ToLowerInvariant());
                }

                _interruptMonitors.Add(gpioId, new InterruptMonitor());
            }
        }

        void PollGpios()
        {
            // Consider using  Interop.epoll_wait. But this will require a thread per GPIO.

            var eventArgsList = new List<GpioAdapterStateChangedEventArgs>(_interruptMonitors.Count);

            while (true)
            {
                try
                {
                    lock (_interruptMonitors)
                    {
                        eventArgsList.Clear();

                        foreach (var interruptMonitor in _interruptMonitors)
                        {
                            var currentState = ReadGpioState(interruptMonitor.Key);
                            if (currentState == interruptMonitor.Value.LatestState)
                            {
                                continue;
                            }

                            eventArgsList.Add(new GpioAdapterStateChangedEventArgs
                            {
                                GpioId = interruptMonitor.Key,
                                OldState = interruptMonitor.Value.LatestState,
                                NewState = currentState
                            });

                            interruptMonitor.Value.LatestState = currentState;
                            interruptMonitor.Value.Timestamp = DateTime.UtcNow;
                        }
                    }

                    foreach (var eventArgs in eventArgsList)
                    {
                        OnGpioChanged(eventArgs);
                    }
                }
                catch (ThreadAbortException)
                {
                }
                catch (Exception exception)
                {
                    _logger.Log(LogLevel.Error, exception, "Error while polling interrupt inputs.");
                }
                finally
                {
                    Thread.Sleep(50);
                }
            }
        }

        static void ExportGpio(int gpioId)
        {
            var path = "/sys/class/gpio/gpio" + gpioId;
            if (Directory.Exists(path))
            {
                return;
            }

            File.WriteAllText("/sys/class/gpio/export", gpioId.ToString(), Encoding.ASCII);
        }

        static void WriteGpioDirection(int gpioId, string direction)
        {
            var path = "/sys/class/gpio/gpio" + gpioId + "/direction";
            File.WriteAllText(path, direction, Encoding.ASCII);
        }

        static GpioState ReadGpioState(int gpioId)
        {
            var path = "/sys/class/gpio/gpio" + gpioId + "/value";
            var gpioValue = File.ReadAllText(path, Encoding.ASCII);

            return gpioValue.Trim() == "1" ? GpioState.High : GpioState.Low;
        }

        static void WrtieGpioState(int gpioId, GpioState state)
        {
            var fileContent = state == GpioState.Low ? "0" : "1";
            File.WriteAllText("/sys/class/gpio/gpio" + gpioId + "/value", fileContent, Encoding.ASCII);
        }

        void OnGpioChanged(GpioAdapterStateChangedEventArgs eventArgs)
        {
            _logger.Log(LogLevel.Information, $"GPIO {eventArgs.GpioId} changed from {eventArgs.OldState} to {eventArgs.NewState}.");

            GpioStateChanged?.Invoke(this, eventArgs);
        }
    }
}
