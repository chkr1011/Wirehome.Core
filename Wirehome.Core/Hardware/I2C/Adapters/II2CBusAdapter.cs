namespace Wirehome.Core.Hardware.I2C.Adapters;

public interface II2CBusAdapter
{
    int Read(int deviceAddress, byte[] buffer, int length);

    int Write(int deviceAddress, byte[] buffer, int length);

    int WriteRead(int deviceAddress, byte[] writeBuffer, int writeBufferLength, byte[] readBuffer, int readBufferLength);
}