using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Wirehome.Core.System;

namespace Wirehome.Core.Hardware.GPIO.Adapters
{
    public sealed class LinuxGpioAdapter : IGpioAdapter
    {
        readonly Dictionary<int, InterruptMonitor> _interruptMonitors = new();
        readonly ILogger _logger;

        readonly BlockingCollection<GpioAdapterStateChangedEventArgs> _pendingEvents = new();

        readonly object _syncRoot = new();
        readonly SystemCancellationToken _systemCancellationToken;

        Thread _eventThread;
        Thread _workerThread;

        public LinuxGpioAdapter(SystemCancellationToken systemCancellationToken, ILogger logger)
        {
            _systemCancellationToken = systemCancellationToken ?? throw new ArgumentNullException(nameof(systemCancellationToken));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public event EventHandler<GpioAdapterStateChangedEventArgs> GpioStateChanged;

        public void Enable()
        {
            // Do not use tasks because polling must be fast as possible and task library overhead (continuation,
            // cancellation, async/await, thread pool) is not needed.
            _workerThread = new Thread(PollGpios)
            {
                IsBackground = true
            };

            _workerThread.Start();

            _eventThread = new Thread(FireEvents)
            {
                IsBackground = true
            };

            _eventThread.Start();

            _logger.LogInformation("Started GPIO polling thread.");
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

        public GpioState ReadState(int gpioId)
        {
            lock (_syncRoot)
            {
                return ReadGpioState(gpioId);
            }
        }

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

        static void ExportGpio(int gpioId)
        {
            var path = "/sys/class/gpio/gpio" + gpioId;
            if (Directory.Exists(path))
            {
                return;
            }

            File.WriteAllText("/sys/class/gpio/export", gpioId.ToString(), Encoding.ASCII);
        }

        void FireEvents()
        {
            while (!_systemCancellationToken.Token.IsCancellationRequested)
            {
                try
                {
                    var eventArgs = _pendingEvents.Take(_systemCancellationToken.Token);
                    OnGpioChanged(eventArgs);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    _logger.Log(LogLevel.Error, exception, "Error while firing GPIO state changed event.");
                }
            }
        }

        void OnGpioChanged(GpioAdapterStateChangedEventArgs eventArgs)
        {
            _logger.Log(LogLevel.Information, $"Interrupt GPIO {eventArgs.GpioId} changed from {eventArgs.OldState} to {eventArgs.NewState}.");

            GpioStateChanged?.Invoke(this, eventArgs);
        }

        void PollGpios()
        {
            // Consider using  Interop.epoll_wait. But this will require a thread per GPIO.

            while (!_systemCancellationToken.Token.IsCancellationRequested)
            {
                try
                {
                    lock (_interruptMonitors)
                    {
                        foreach (var interruptMonitor in _interruptMonitors)
                        {
                            var currentState = ReadGpioState(interruptMonitor.Key);
                            if (currentState == interruptMonitor.Value.LatestState)
                            {
                                continue;
                            }

                            _pendingEvents.Add(new GpioAdapterStateChangedEventArgs
                            {
                                GpioId = interruptMonitor.Key,
                                OldState = interruptMonitor.Value.LatestState,
                                NewState = currentState
                            });

                            interruptMonitor.Value.LatestState = currentState;
                            interruptMonitor.Value.Timestamp = DateTime.UtcNow;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
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
                    Thread.Sleep(10);
                }
            }
        }

        static GpioState ReadGpioState(int gpioId)
        {
            var path = "/sys/class/gpio/gpio" + gpioId + "/value";
            var gpioValue = File.ReadAllText(path, Encoding.ASCII);

            return gpioValue.Trim() == "1" ? GpioState.High : GpioState.Low;
        }

        static void WriteGpioDirection(int gpioId, string direction)
        {
            var path = "/sys/class/gpio/gpio" + gpioId + "/direction";
            File.WriteAllText(path, direction, Encoding.ASCII);
        }

        static void WrtieGpioState(int gpioId, GpioState state)
        {
            var fileContent = state == GpioState.Low ? "0" : "1";
            File.WriteAllText("/sys/class/gpio/gpio" + gpioId + "/value", fileContent, Encoding.ASCII);
        }
    }
}