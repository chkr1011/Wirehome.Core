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
        readonly Dictionary<int, FileStream> _openGpios = new Dictionary<int, FileStream>();

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
            lock (_syncRoot)
            {
                if (!_openGpios.TryGetValue(gpioId, out var valueFile))
                {
                    var path = "/sys/class/gpio/gpio" + gpioId + "/value";
                    valueFile = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    _openGpios[gpioId] = valueFile;
                }

                return ReadGpioState(valueFile);
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

                _interruptMonitors.Add(gpioId, new InterruptMonitor
                {
                    LatestState = null,
                    ValuePath = valuePath,
                    ValueFile = File.Open(valuePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
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

            var eventArgsList = new List<GpioAdapterStateChangedEventArgs>(_interruptMonitors.Count);

            while (true)
            {
                try
                {
                    var hasInterruptMonitors = false;

                    lock (_interruptMonitors)
                    {
                        hasInterruptMonitors = _interruptMonitors.Count > 0;

                        eventArgsList.Clear();

                        foreach (var interruptMonitor in _interruptMonitors)
                        {
                            var currentState = ReadGpioState(interruptMonitor.Value.ValueFile);
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

                    if (hasInterruptMonitors)
                    {
                        Thread.Sleep(10);
                    }
                    else
                    {
                        // Do not waste CPU time if this feature is not used.
                        Thread.Sleep(1000);
                    }
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

        GpioState ReadGpioState(FileStream fileStream)
        {
            var buffer = new byte[1];

            fileStream.Seek(0, SeekOrigin.Begin);
            fileStream.Read(buffer, 0, 1);

            if (buffer[0] == (byte)'1')
            {
                return GpioState.High;
            }

            return GpioState.Low;
        }

        void OnGpioChanged(GpioAdapterStateChangedEventArgs eventArgs)
        {
            _logger.Log(LogLevel.Information, "GPIO {0} changed from {1} to {2}.", eventArgs.GpioId, eventArgs.OldState, eventArgs.NewState);
            GpioStateChanged?.Invoke(this, eventArgs);
        }
    }
}
