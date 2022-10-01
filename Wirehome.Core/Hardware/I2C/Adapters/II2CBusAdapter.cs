namespace Wirehome.Core.Hardware.I2C.Adapters
{
    public interface II2CBusAdapter
    {
        int Read(int deviceAddress, byte[] buffer);
        
        int Write(int deviceAddress, byte[] buffer);

        int WriteRead(int deviceAddress, byte[] writeBuffer, byte[] readBuffer);
    }
}