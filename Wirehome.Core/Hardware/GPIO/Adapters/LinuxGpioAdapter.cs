using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Wirehome.Core.System;

namespace Wirehome.Core.Hardware.GPIO.Adapters;

public sealed class LinuxGpioAdapter : IGpioAdapter
{
    readonly ConcurrentDictionary<int, Gpio> _gpios = new();
    readonly ConcurrentDictionary<int, GpioInterruptMonitor> _interruptMonitors = new();
    readonly ILogger _logger;
    readonly BlockingCollection<GpioAdapterStateChangedEventArgs> _pendingEvents = new();

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
        _workerThread = new Thread(PollGpios) { IsBackground = true };

        _workerThread.Start();

        _eventThread = new Thread(FireEvents) { IsBackground = true };

        _eventThread.Start();

        _logger.LogInformation("Started GPIO polling thread.");
    }

    public void EnableInterrupt(int gpioId, GpioInterruptEdge edge)
    {
        if (_interruptMonitors.ContainsKey(gpioId))
        {
            return;
        }

        if (!_gpios.TryGetValue(gpioId, out var gpio))
        {
            gpio = ExportGpio(gpioId);
        }

        gpio.SetDirection(GpioDirection.Input);

        _interruptMonitors[gpioId] = new GpioInterruptMonitor(gpio);
    }

    public GpioState ReadState(int gpioId)
    {
        if (!_gpios.TryGetValue(gpioId, out var gpio))
        {
            gpio = ExportGpio(gpioId);
        }

        return gpio.Read();
    }

    public void SetDirection(int gpioId, GpioDirection direction)
    {
        if (!_gpios.TryGetValue(gpioId, out var gpio))
        {
            gpio = ExportGpio(gpioId);
        }

        gpio.SetDirection(direction);
        
        _logger.LogInformation($"GPIO {gpioId} direction set to '{direction}'.");
    }

    public void WriteState(int gpioId, GpioState state)
    {
        if (!_gpios.TryGetValue(gpioId, out var gpio))
        {
            gpio = ExportGpio(gpioId);
        }

        gpio.Write(state);
    }

    Gpio ExportGpio(int gpioId)
    {
        // var path = "/sys/class/gpio/gpio" + gpioId;
        // if (Directory.Exists(path))
        // {
        //     throw new InvalidOperationException($"GPIO {gpioId} is invalid");
        // }

        File.WriteAllText("/sys/class/gpio/export", gpioId.ToString(), Encoding.ASCII);

        var gpio = new Gpio(gpioId);
        _gpios[gpioId] = gpio;

        _logger.LogInformation($"Exported GPIO {gpioId}.");

        return gpio;
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
                foreach (var interruptMonitor in _interruptMonitors)
                {
                    if (!interruptMonitor.Value.Poll(out var oldState, out var newState))
                    {
                        continue;
                    }

                    _pendingEvents.Add(new GpioAdapterStateChangedEventArgs { GpioId = interruptMonitor.Key, OldState = oldState, NewState = newState });
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
}