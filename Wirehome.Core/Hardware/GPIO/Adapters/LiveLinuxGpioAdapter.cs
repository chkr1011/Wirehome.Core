using Microsoft.Extensions.Logging;
using System;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Threading;
using System.Threading.Tasks;

namespace Wirehome.Core.Hardware.GPIO.Adapters
{
    public sealed class LiveLinuxGpioAdapter : IGpioAdapter, IDisposable
    {
        readonly GpioController _gpioController = new GpioController(PinNumberingScheme.Logical, UnixDriver.Create());
        private readonly ILogger _logger;

        public event EventHandler<GpioAdapterStateChangedEventArgs> GpioStateChanged;

        public LiveLinuxGpioAdapter(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void EnableInterrupt(int gpioId, GpioInterruptEdge edge)
        {
            if (!_gpioController.IsPinOpen(gpioId))
            {
                _gpioController.OpenPin(gpioId, PinMode.Input);
            }

            Task.Run(() => MonitorGpio(gpioId, CancellationToken.None));
        }

        private async Task MonitorGpio(int gpioId, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _gpioController.WaitForEventAsync(gpioId, PinEventTypes.Falling | PinEventTypes.Rising, cancellationToken).ConfigureAwait(false);
                    if (result.TimedOut)
                    {
                        continue;
                    }

                    OnPinChanged(gpioId, result.EventTypes);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, $"Error while monitoring GPIO {gpioId}.");
                }
            }
        }

        public GpioState ReadState(int gpioId)
        {
            if (!_gpioController.IsPinOpen(gpioId))
            {
                _gpioController.OpenPin(gpioId, PinMode.Input);
            }

            return _gpioController.Read(gpioId) == PinValue.High ? GpioState.High : GpioState.Low;
        }

        public void SetDirection(int gpioId, GpioDirection direction)
        {
            if (direction == GpioDirection.Input)
            {
                _gpioController.SetPinMode(gpioId, PinMode.Input);
            }
            else if (direction == GpioDirection.Output)
            {
                _gpioController.SetPinMode(gpioId, PinMode.Output);
            }
        }

        public void WriteState(int gpioId, GpioState state)
        {
            if (!_gpioController.IsPinOpen(gpioId))
            {
                _gpioController.OpenPin(gpioId, PinMode.Output);
            }

            var targetState = state == GpioState.High ? PinValue.High : PinValue.Low;
            _gpioController.Write(gpioId, targetState);
        }

        public void Dispose()
        {
            _gpioController.Dispose();
        }

        void OnPinChanged(int gpioId, PinEventTypes changeType)
        {
            var oldState = GpioState.Low;
            var newState = GpioState.Low;

            if (changeType == PinEventTypes.Falling)
            {
                oldState = GpioState.High;
            }
            else if (changeType == PinEventTypes.Rising)
            {
                newState = GpioState.High;
            }

            GpioStateChanged?.Invoke(this, new GpioAdapterStateChangedEventArgs
            {
                GpioId = gpioId,
                OldState = oldState,
                NewState = newState
            });
        }
    }
}
