namespace Wirehome.Core.Hardware.GPIO.Adapters
{
    public sealed class GpioInterruptMonitor
    {
        readonly Gpio _gpio;
        
        GpioState _currentState;
        
        public GpioInterruptMonitor(Gpio gpio)
        {
            _gpio = gpio;

            // Started as pull up.
            _currentState = GpioState.High;
        }
        
        public bool Poll(out GpioState oldState, out GpioState newState)
        {
            oldState = _currentState;
            newState = _gpio.Read();

            if (newState == _currentState)
            {
                return false;
            }

            _currentState = newState;
            
            return true;
        }
    }
}