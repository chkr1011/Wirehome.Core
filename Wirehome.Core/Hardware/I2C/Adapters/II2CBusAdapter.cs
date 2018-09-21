using System;

namespace Wirehome.Core.Hardware.I2C.Adapters
{
    public interface II2CBusAdapter
    {
        void Write(int deviceAddress, ArraySegment<byte> buffer);

        void Read(int deviceAddress, ArraySegment<byte> buffer);

        void WriteRead(int deviceAddress, ArraySegment<byte> writeBuffer, ArraySegment<byte> readBuffer);
    }
}
