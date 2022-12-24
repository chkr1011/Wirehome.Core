namespace Wirehome.Core.Hardware.GPIO.Adapters;

public sealed class GpioStateChangedEventArgs : GpioAdapterStateChangedEventArgs
{
    public string HostId { get; set; }
}